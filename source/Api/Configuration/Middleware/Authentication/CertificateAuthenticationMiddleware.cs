// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.Configuration.Middleware.Authentication
{
    public class CertificateAuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<CertificateAuthenticationMiddleware> _logger;

        public CertificateAuthenticationMiddleware(ILogger<CertificateAuthenticationMiddleware> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));

            var httpRequestData = context.GetHttpRequestData();
            if (httpRequestData == null)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            if (httpRequestData.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                contentTypeValues = contentTypeValues.ToArray();
                if (contentTypeValues.SingleOrDefault() != "application/ebix")
                {
                    _logger.LogTrace("Content-Type was not application/ebix (actual value: \"{ContentType}\"), skipping certificate authentication", string.Join(", ", contentTypeValues));
                    await next(context).ConfigureAwait(false);
                    return;
                }
            }

            var hasCertificate = httpRequestData.Headers.TryGetValues("ClientCert", out var certificateHeader);
            var certificates = certificateHeader?.ToList();
            if (!hasCertificate || certificates == null || certificates.Count != 1)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            using var certificate = new X509Certificate2(Convert.FromBase64String(certificates.Single()));
            var thumbprint = certificate.Thumbprint;

            // TODO: Validate against actors certificate thumbprints in database. If actor found, build claims principal and set it on CurrentClaimsPrincipal
            // var result = getClaimsPrincipalFromCertificate(thumbprint);
            // if (result.Success == false)
            // {
            //     LogParseResult(result);
            //     return;
            // }

            // var currentClaimsPrincipal = context.GetService<CurrentClaimsPrincipal>();
            // currentClaimsPrincipal.SetCurrentUser(result.ClaimsPrincipal!);
            // _logger.LogInformation("Bearer token authentication succeeded.");
            // await next(context).ConfigureAwait(false);
        }

        private void LogParseResult(Result result)
        {
            var message = new StringBuilder();
            message.AppendLine("Failed to parse claims principal from JWT:");
            message.AppendLine(result.Error?.Message);
            message.AppendLine("Token from HTTP request header:");
            message.AppendLine(result.Token);
            _logger.LogError(message.ToString());
        }
    }
}
