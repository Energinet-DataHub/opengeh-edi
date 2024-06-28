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
using System.ServiceModel.Security;
using System.Xml;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

internal sealed class EbixRequestDsl
{
    private readonly EbixDriver _ebix;

#pragma warning disable VSTHRD200 // Since this is a DSL we don't want to suffix tasks with 'Async' since it is not part of the ubiquitous language
    public EbixRequestDsl(EbixDriver ebix)
    {
        _ebix = ebix;
    }

    #pragma warning disable VSTHRD200

    internal async Task EmptyQueueForActor()
    {
        await _ebix.EmptyQueueAsync().ConfigureAwait(false);
    }

    internal async Task ConfirmEnergyResultIsAvailable()
    {
        var response = await _ebix.PeekMessageAsync().ConfigureAwait(false);

        await _ebix.DequeueMessageAsync(GetMessageIdForAggregatedMeasuredDataTimeSeries(response!)).ConfigureAwait(false);

        Assert.Multiple(
            () => Assert.NotNull(response?.MessageContainer?.Payload),
            () => Assert.Equal("AggregatedMeteredDataTimeSeries", response?.MessageContainer?.DocumentType));
    }

    internal async Task ConfirmWholesaleResultIsAvailable()
    {
        var response = await _ebix.PeekMessageAsync().ConfigureAwait(false);

        await _ebix.DequeueMessageAsync(GetMessageIdForNotifyAggregatedWholesaleServices(response!)).ConfigureAwait(false);

        Assert.Multiple(
            () => Assert.NotNull(response?.MessageContainer?.Payload),
            () => Assert.Equal("NotifyAggregatedWholesaleServices", response?.MessageContainer?.DocumentType));
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

        using var assertionScope = new AssertionScope();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseBody.Should().Contain("<faultstring>B2B-900").And.Contain("<faultcode>soap-env:Client");
    }

    internal async Task ConfirmDequeueWithIncorrectMessageIdGivesEbixError()
    {
        var response = await _ebix.DequeueMessageWithoutCertificateAsync().ConfigureAwait(false);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode),
            () => Assert.Contains("Certificate rejected", response.ReasonPhrase, StringComparison.InvariantCultureIgnoreCase));
    }

    internal async Task ConfirmPeekWithRemovedCertificateIsNotAllowed()
    {
        var response = await _ebix.PeekMessageWithoutCertificateAsync().ConfigureAwait(false);

        Assert.Multiple(
             () => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode),
             () => Assert.Contains("Certificate rejected", response.ReasonPhrase, StringComparison.InvariantCultureIgnoreCase));
    }

    internal async Task ConfirmDequeueWithRemovedCertificateIsNotAllowed()
    {
        var act = async () => await _ebix.DequeueMessageAsync("irrelevant-message-id").ConfigureAwait(false);

        await Assert.ThrowsAsync<MessageSecurityException>(act).ConfigureAwait(false);
    }

    private static string GetMessageIdForAggregatedMeasuredDataTimeSeries(peekMessageResponse response)
    {
        var nsmgr = new XmlNamespaceManager(new NameTable());
        nsmgr.AddNamespace("ns0", "un:unece:260:data:EEM-DK_AggregatedMeteredDataTimeSeries:v3");
        var query = "/ns0:HeaderEnergyDocument/ns0:Identification";
        var node = response.MessageContainer.Payload.SelectSingleNode(query, nsmgr);
        return node!.InnerText;
    }

    private static string GetMessageIdForNotifyAggregatedWholesaleServices(peekMessageResponse response)
    {
        var nsmgr = new XmlNamespaceManager(new NameTable());
        nsmgr.AddNamespace("ns0", "un:unece:260:data:EEM-DK_NotifyAggregatedWholesaleServices:v3");
        var query = "/ns0:HeaderEnergyDocument/ns0:Identification";
        var node = response.MessageContainer.Payload.SelectSingleNode(query, nsmgr);
        return node!.InnerText;
    }
}
