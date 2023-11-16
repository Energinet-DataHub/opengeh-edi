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
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
public sealed class WhenEbixPeekRequestIsReceivedTests : TestRunner
{
    private readonly ApiManagementDriver _apiManagement;
    private readonly EdiDriver _edi;
    private readonly WholeSaleDriver _wholesale;

    public WhenEbixPeekRequestIsReceivedTests()
    {
        _edi = new EdiDriver(AzpToken);
        _wholesale = new WholeSaleDriver(EventPublisher);
        _apiManagement = new ApiManagementDriver(AzureEntraTenantId, AzureEntraBackendAppId);
    }

    [Fact]
    public async Task Actor_can_peek_calculation_result_in_ebix_format()
    {
        var token = await _apiManagement.GetAzureAdToken(AzureEntraClientId, AzureEntraClientSecret);

        await _edi.EmptyQueueAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, token);
        await _wholesale.PublishAggregationResultAsync("543", TestRunner.BalanceResponsibleActorNumber);

        var actualResponseString = await _apiManagement.PeekEbixDocumentWithTimeoutAsync(token);

        Assert.Multiple(
            () => Assert.NotEmpty(actualResponseString),
            () => Assert.Contains("<soap-env:Envelope", actualResponseString, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains(
                "<DocumentType xmlns=\"urn:www:datahub:dk:b2b:v01\">AggregatedMeteredDataTimeSeries</DocumentType>",
                actualResponseString,
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Actor_can_peek_accepted_request_in_ebix_format()
    {
        var token = await _apiManagement.GetAzureAdToken(AzureEntraClientId, AzureEntraClientSecret);

        await _edi.EmptyQueueAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, token);
        await _edi.RequestAggregatedMeasureDataAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, overrideToken: token);

        var actualResponseString = await _apiManagement.PeekEbixDocumentWithTimeoutAsync(token);

        Assert.Multiple(
            () => Assert.NotEmpty(actualResponseString),
            () => Assert.Contains("<soap-env:Envelope", actualResponseString, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains(
                "<DocumentType xmlns=\"urn:www:datahub:dk:b2b:v01\">AcceptRequestMeteredDataAggregated</DocumentType>",
                actualResponseString,
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Actor_can_peek_synchronous_rejected_request_in_ebix_format()
    {
        var token = await _apiManagement.GetAzureAdToken(AzureEntraClientId, AzureEntraClientSecret);

        await _edi.EmptyQueueAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, token);
        await _edi.RequestAggregatedMeasureDataAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, overrideToken: token, syncError: true);

        var actualResponseString = await _apiManagement.PeekEbixDocumentWithTimeoutAsync(token);

        Assert.Multiple(
            () => Assert.NotEmpty(actualResponseString),
            () => Assert.Contains("<soap-env:Envelope", actualResponseString, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains(
                "<DocumentType xmlns=\"urn:www:datahub:dk:b2b:v01\">RejectRequestMeteredDataAggregated</DocumentType>",
                actualResponseString,
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Actor_can_peek_asynchronous_rejected_request_in_ebix_format()
    {
        var token = await _apiManagement.GetAzureAdToken(AzureEntraClientId, AzureEntraClientSecret);

        await _edi.EmptyQueueAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, token);
        await _edi.RequestAggregatedMeasureDataAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, overrideToken: token, asyncError: true);

        var actualResponseString = await _apiManagement.PeekEbixDocumentWithTimeoutAsync(token);

        Assert.Multiple(
            () => Assert.NotEmpty(actualResponseString),
            () => Assert.Contains("<soap-env:Envelope", actualResponseString, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains(
                "<DocumentType xmlns=\"urn:www:datahub:dk:b2b:v01\">RejectRequestMeteredDataAggregated</DocumentType>",
                actualResponseString,
                StringComparison.OrdinalIgnoreCase));
    }
}
