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
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
public sealed class WhenEbixPeekRequestIsReceivedTests : TestRunner
{
    private readonly ApiManagementDriver _apiManagement;
    private readonly EdiDriver _edi;
    private readonly WholeSaleDriver _wholesale;
    private readonly EbixDriver _ebixDriver;

    public WhenEbixPeekRequestIsReceivedTests()
    {
        _edi = new EdiDriver(AzpToken);
        _wholesale = new WholeSaleDriver(EventPublisher);
        _apiManagement = new ApiManagementDriver(AzureEntraTenantId, AzureEntraBackendAppId);

        var apiManagementBaseUri = new Uri(ApiManagementDriver.ApiManagementUrl);

        _ebixDriver = new EbixDriver(new Uri(apiManagementBaseUri, "/ebix"));
    }

    [Fact]
    public async Task Actor_can_peek_calculation_result_in_ebix_format()
    {
        var token = await _apiManagement.GetAzureAdToken(AzureEntraClientId, AzureEntraClientSecret);

        await _edi.EmptyQueueAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, token);
        await _wholesale.PublishAggregationResultAsync("543", TestRunner.BalanceResponsibleActorNumber);

        var actualResponse = await _ebixDriver.PeekMessageWithTimeoutAsync(token);

        Assert.Multiple(
            () => Assert.NotNull(actualResponse?.MessageContainer?.Payload),
            () => Assert.Equal("AggregatedMeteredDataTimeSeries", actualResponse!.MessageContainer.DocumentType));
    }

    [Fact]
    public async Task Actor_can_peek_accepted_request_in_ebix_format()
    {
        var token = await _apiManagement.GetAzureAdToken(AzureEntraClientId, AzureEntraClientSecret);

        await _edi.EmptyQueueAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, token);
        await _edi.RequestAggregatedMeasureDataAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, overrideToken: token);

        var actualResponse = await _ebixDriver.PeekMessageWithTimeoutAsync(token);

        Assert.Multiple(
            () => Assert.NotNull(actualResponse?.MessageContainer?.Payload),
            () => Assert.Equal("AcceptRequestMeteredDataAggregated", actualResponse!.MessageContainer.DocumentType));
    }

    [Fact]
    public async Task Actor_can_peek_rejected_request_in_ebix_format()
    {
        var token = await _apiManagement.GetAzureAdToken(AzureEntraClientId, AzureEntraClientSecret);

        await _edi.EmptyQueueAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, token);
        await _edi.RequestAggregatedMeasureDataAsync(TestRunner.BalanceResponsibleActorNumber, new[] { TestRunner.BalanceResponsibleActorRole }, overrideToken: token, asyncError: true);

        var actualResponse = await _ebixDriver.PeekMessageWithTimeoutAsync(token);

        Assert.Multiple(
            () => Assert.NotNull(actualResponse?.MessageContainer?.Payload),
            () => Assert.Equal("RejectRequestMeteredDataAggregated", actualResponse!.MessageContainer.DocumentType));
    }
}
