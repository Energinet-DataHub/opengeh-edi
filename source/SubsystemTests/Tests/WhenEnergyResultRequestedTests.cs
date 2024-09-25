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
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C;
using Energinet.DataHub.EDI.SubsystemTests.Dsl;
using NodaTime;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests;

[SuppressMessage(
    "Usage",
    "CA2007",
    Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
[IntegrationTest]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public sealed class WhenEnergyResultRequestedTests : BaseTestClass
{
    private readonly NotifyAggregatedMeasureDataResultDsl _notifyAggregatedMeasureDataResult;
    private readonly AggregatedMeasureDataRequestDsl _aggregatedMeasureDataRequest;
    private readonly string _energySupplierActorNumber;

    public WhenEnergyResultRequestedTests(AcceptanceTestFixture fixture, ITestOutputHelper output)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var ediDriver = new EdiDriver(fixture.DurableClient, fixture.B2BClients.EnergySupplier, output);
        var wholesaleDriver = new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient);

        _notifyAggregatedMeasureDataResult = new NotifyAggregatedMeasureDataResultDsl(
            ediDriver,
            wholesaleDriver);

        _aggregatedMeasureDataRequest =
            new AggregatedMeasureDataRequestDsl(
                ediDriver,
                new B2CEdiDriver(fixture.B2CClients.EnergySupplier, fixture.ApiManagementUri, fixture.EdiB2CWebApiUri, output),
                new EdiDatabaseDriver(fixture.ConnectionString),
                wholesaleDriver);

        _energySupplierActorNumber = AcceptanceTestFixture.EdiSubsystemTestCimEnergySupplierNumber;
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
    public async Task B2C_actor_can_request_aggregated_measure_data()
    {
        var createdAfter = SystemClock.Instance.GetCurrentInstant();
        var energySupplierActorNumber = _energySupplierActorNumber;
        await _aggregatedMeasureDataRequest.B2CRequest(cancellationToken: CancellationToken.None);

        await _aggregatedMeasureDataRequest.ConfirmRequestIsInitialized(
            createdAfter,
            requestedByActorNumber: energySupplierActorNumber);
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
