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

using System.Net;
using System.ServiceModel;
using System.Xml;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

internal sealed class EbixRequestDsl
{
    private readonly EdiDriver _edi;
    private readonly WholesaleDriver _wholesale;
    private readonly EbixDriver _ebix;

    public EbixRequestDsl(EdiDriver edi, WholesaleDriver wholesale, EbixDriver ebix)
    {
        _edi = edi;
        _wholesale = wholesale;
        _ebix = ebix;
    }

    #pragma warning disable VSTHRD200

    internal async Task EmptyQueueForActor(string actorNumber, string actorRole)
    {
        await _edi.EmptyQueueAsync(actorNumber, new[] { actorRole, }).ConfigureAwait(false);
    }

    internal Task PublishAggregationResultFor(string gridArea)
    {
        return _wholesale.PublishAggregationResultAsync(gridArea);
    }

    internal async Task ConfirmEbixResultIsAvailableForActor()
    {
        var response = await _ebix.PeekMessageAsync(timeoutInSeconds: 60).ConfigureAwait(false);

        await _ebix.DequeueMessageAsync(GetMessageId(response!)).ConfigureAwait(false);

        Assert.Multiple(
            () => Assert.NotNull(response?.MessageContainer?.Payload),
            () => Assert.Equal("AggregatedMeteredDataTimeSeries", response?.MessageContainer?.DocumentType));
    }

    internal async Task ConfirmPeekWithoutCertificateIsNotAllowed()
    {
        var response = await _ebix.PeekMessageWithoutCertificateAsync().ConfigureAwait(false);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode),
            () => Assert.Contains("Certificate rejected", response.ReasonPhrase, StringComparison.InvariantCultureIgnoreCase));
    }

    internal async Task ConfirmDequeueWithoutCertificateIsNotAllowed()
    {
        var response = await _ebix.DequeueMessageWithoutCertificateAsync().ConfigureAwait(false);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode),
            () => Assert.Contains("Certificate rejected", response.ReasonPhrase, StringComparison.InvariantCultureIgnoreCase));
    }

    internal async Task ConfirmInvalidDequeueRequestGivesEbixError()
    {
        var response = await _ebix.DequeueWithoutRequestBodyAsync().ConfigureAwait(false);

        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode),
            () => Assert.Contains("<faultstring>B2B-900", responseBody, StringComparison.InvariantCulture),
            () => Assert.Contains("<faultcode>soap-env:Client", responseBody, StringComparison.InvariantCulture));
    }

    internal async Task ConfirmDequeueWithIncorrectMessageIdGivesEbixError()
    {
        var act = () => _ebix.DequeueMessageAsync("incorrect-message-id");

        var thrownException = await Assert.ThrowsAsync<FaultException>(act).ConfigureAwait(false);

        Assert.StartsWith("B2B-201:", thrownException.Reason.ToString(), StringComparison.InvariantCulture);
    }

    private static string GetMessageId(peekMessageResponse response)
    {
        var nsmgr = new XmlNamespaceManager(new NameTable());
        nsmgr.AddNamespace("ns0", "un:unece:260:data:EEM-DK_AggregatedMeteredDataTimeSeries:v3");
        var query = "/ns0:HeaderEnergyDocument/ns0:Identification";
        var node = response.MessageContainer.Payload.SelectSingleNode(query, nsmgr);
        return node!.InnerText;
    }
}
