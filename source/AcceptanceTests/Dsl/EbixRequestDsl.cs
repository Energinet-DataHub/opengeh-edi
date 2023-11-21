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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

internal sealed class EbixRequestDsl
{
    private readonly AzureAuthenticationDriver _azureAuthentication;
    private readonly EdiDriver _edi;
    private readonly WholesaleDriver _wholesale;
    private readonly EbixDriver _ebix;

    public EbixRequestDsl(AzureAuthenticationDriver azureAuthentication, EdiDriver edi, WholesaleDriver wholesale, EbixDriver ebix)
    {
        _azureAuthentication = azureAuthentication;
        _edi = edi;
        _wholesale = wholesale;
        _ebix = ebix;
    }

    #pragma warning disable VSTHRD200

    internal async Task EmptyQueueForActor(string actorNumber, string actorRole, string token)
    {
        await _edi.EmptyQueueAsync(actorNumber, new[] { actorRole, }, token).ConfigureAwait(false);
    }

    internal Task<string> LoginAsActor(string azureEntraClientId, string azureEntraClientSecret)
    {
        return _azureAuthentication.GetAzureAdTokenAsync(azureEntraClientId, azureEntraClientSecret);
    }

    internal Task PublishAggregationResultFor(string gridArea, string balanceResponsibleNumber)
    {
        return _wholesale.PublishAggregationResultAsync(gridArea, balanceResponsibleNumber);
    }

    internal async Task ConfirmPeekIsCorrectEbixFormatAndDocumentType(string token)
    {
        var response = await _ebix.PeekMessageWithTimeoutAsync(token).ConfigureAwait(false);

        Assert.Multiple(
            () => Assert.NotNull(response?.MessageContainer?.Payload),
            () => Assert.Equal("AggregatedMeteredDataTimeSeries", response!.MessageContainer.DocumentType));
    }
}
