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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

[SuppressMessage(
    "Usage",
    "CA2007",
    Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
[IntegrationTest]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public sealed class WhenEnergyResultRequestedTests
{
    private readonly NotifyAggregatedMeasureDataResultDsl _notifyAggregatedMeasureDataResult;
    private readonly AggregatedMeasureDataRequestDsl _aggregatedMeasureDataRequest;

    public WhenEnergyResultRequestedTests(AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var ediDriver = new EdiDriver(fixture.B2BEnergySupplierAuthorizedHttpClient);
        var ediProcessesDriver = new EdiProcessesDriver(fixture.ConnectionString);
        var wholesaleDriver = new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient);

        _notifyAggregatedMeasureDataResult = new NotifyAggregatedMeasureDataResultDsl(
            ediDriver,
            wholesaleDriver);

        _aggregatedMeasureDataRequest =
            new AggregatedMeasureDataRequestDsl(ediDriver, ediProcessesDriver, wholesaleDriver);
    }

    [Fact]
    public async Task Actor_can_request_aggregated_measure_data()
    {
        var messageId = await _aggregatedMeasureDataRequest.Request(cancellationToken: CancellationToken.None);

        await _aggregatedMeasureDataRequest.ConfirmRequestIsInitialized(
            messageId,
            CancellationToken.None);
    }

    [Fact]
    public async Task Actor_get_bad_request_when_aggregated_measure_data_request_is_invalid()
    {
        await _aggregatedMeasureDataRequest.ConfirmInvalidRequestIsRejected(CancellationToken.None);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_response_from_aggregated_measure_data_request()
    {
        await _aggregatedMeasureDataRequest.PublishAggregatedMeasureDataRequestAcceptedResponse(
            "804",
            AcceptanceTestFixture.EdiSubsystemTestCimEnergySupplierNumber,
            CancellationToken.None);

        await _notifyAggregatedMeasureDataResult.ConfirmResultIsAvailable();
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_rejected_response_from_aggregated_measure_data_request()
    {
         await _aggregatedMeasureDataRequest.PublishAggregatedMeasureDataRequestRejectedResponse(
            "804",
            AcceptanceTestFixture.EdiSubsystemTestCimEnergySupplierNumber,
            CancellationToken.None);

         await _notifyAggregatedMeasureDataResult.ConfirmRejectResultIsAvailable();
    }
}
