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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Authentication.Certificate;

public class HeaderClientCertificateRetriever : IClientCertificateRetriever
{
    public const string CertificateHeaderName = "ClientCert";
    private readonly ILogger<HeaderClientCertificateRetriever> _logger;

    public HeaderClientCertificateRetriever(ILogger<HeaderClientCertificateRetriever> logger)
    {
        _logger = logger;
    }

    public X509Certificate2? GetCertificate(HttpRequestData httpRequestData)
    {
        ArgumentNullException.ThrowIfNull(httpRequestData);

        var hasCertificate = httpRequestData.Headers.TryGetValues(CertificateHeaderName, out var certificateHeader);
        var certificates = certificateHeader?.ToList();
        if (!hasCertificate || certificates == null || certificates.Count != 1)
        {
            return null;
        }

        X509Certificate2? certificate;

        try
        {
            certificate = new X509Certificate2(Convert.FromHexString(certificates.Single()));
        }
        catch (CryptographicException e)
        {
            _logger.LogWarning(e, "Error creating certificate from header");
            certificate = null;
        }

        return certificate;
    }
}
