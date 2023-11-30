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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Api.Authentication.Certificate;
using Energinet.DataHub.EDI.Api.Common;
using Energinet.DataHub.EDI.Application.ActorCertificate;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.Api.Authentication;

public class CertificateAuthenticationMethod : IAuthenticationMethod
{
    private readonly IClientCertificateRetriever _clientCertificateRetriever;
    private readonly IActorCertificateRepository _actorCertificateRepository;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;

    public CertificateAuthenticationMethod(IClientCertificateRetriever clientCertificateRetriever, IActorCertificateRepository actorCertificateRepository, IMarketActorAuthenticator marketActorAuthenticator)
    {
        _clientCertificateRetriever = clientCertificateRetriever;
        _actorCertificateRepository = actorCertificateRepository;
        _marketActorAuthenticator = marketActorAuthenticator;
    }

    public bool ShouldHandle(HttpRequestData httpRequestData)
    {
        ArgumentNullException.ThrowIfNull(httpRequestData);

        var contentType = httpRequestData.Headers.GetContentType();
        return contentType == "application/ebix";
    }

    public async Task<bool> AuthenticateAsync(HttpRequestData httpRequestData, CancellationToken cancellationToken)
    {
        var certificate = _clientCertificateRetriever.GetCertificate(httpRequestData);
        if (certificate == null)
            return false;

        var actorCertificate = await _actorCertificateRepository.GetFromThumbprintAsync(certificate.Thumbprint).ConfigureAwait(false);

        if (actorCertificate == null)
            return false;

        return _marketActorAuthenticator.Authenticate(actorCertificate.ActorNumber, actorCertificate.ActorRole);
    }
}
