﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.B2BApi.Authentication.Certificate;
using Energinet.DataHub.EDI.B2BApi.Common;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.B2BApi.Authentication;

public class CertificateAuthenticationMethod : IAuthenticationMethod
{
    private readonly IClientCertificateRetriever _clientCertificateRetriever;
    private readonly IMasterDataClient _masterDataClient;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;

    public CertificateAuthenticationMethod(
        IClientCertificateRetriever clientCertificateRetriever,
        IMasterDataClient masterDataClient,
        IMarketActorAuthenticator marketActorAuthenticator)
    {
        _clientCertificateRetriever = clientCertificateRetriever;
        _masterDataClient = masterDataClient;
        _marketActorAuthenticator = marketActorAuthenticator;
    }

    public bool ShouldHandle(HttpRequestData httpRequestData)
    {
        ArgumentNullException.ThrowIfNull(httpRequestData);

        var contentType = httpRequestData.Headers.TryGetContentType();
        return contentType == "application/ebix";
    }

    public async Task<bool> AuthenticateAsync(HttpRequestData httpRequestData, CancellationToken cancellationToken)
    {
        var certificate = _clientCertificateRetriever.GetCertificate(httpRequestData);
        if (certificate == null)
            return false;

        var actor = await _masterDataClient
            .GetActorFromThumbprintAsync(new CertificateThumbprintDto(certificate.Thumbprint))
            .ConfigureAwait(false);

        if (actor == null)
        {
            return false;
        }

        return _marketActorAuthenticator.Authenticate(actor.ActorNumber, actor.ActorRole, null);
    }
}
