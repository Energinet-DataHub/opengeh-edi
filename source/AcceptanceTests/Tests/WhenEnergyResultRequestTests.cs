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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

[SuppressMessage(
    "Usage",
    "CA2007",
    Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
[IntegrationTest]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public sealed class WhenEnergyResultRequestTests
{
    private readonly NotifyAggregatedMeasureDataResultDsl _notifyAggregatedMeasureDataResultDsl;
    private readonly AggregatedMeasureDataRequestDsl _aggregatedMeasureDataRequestDsl;

    public WhenEnergyResultRequestTests(AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var ediDriver = new EdiDriver(fixture.B2BEnergySupplierAuthorizedHttpClient);
        var ediProcessesDriver = new EdiProcessesDriver(fixture.ConnectionString);

        _notifyAggregatedMeasureDataResultDsl = new NotifyAggregatedMeasureDataResultDsl(
            ediDriver,
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient));

        _aggregatedMeasureDataRequestDsl = new AggregatedMeasureDataRequestDsl(ediDriver, ediProcessesDriver);
    }

    [Fact]
    public async Task Actor_can_request_aggregated_measure_data()
    {
        await _notifyAggregatedMeasureDataResultDsl.EmptyQueueForActor();

        var messageId = await _aggregatedMeasureDataRequestDsl.RequestAsync(cancellationToken: CancellationToken.None);

        await _aggregatedMeasureDataRequestDsl.ConfirmRequestIsInitiatedAsync(
            messageId,
            CancellationToken.None);
    }

    [Fact]
    public async Task Actor_request_invalid_aggregated_measure_data()
    {
        await _notifyAggregatedMeasureDataResultDsl.EmptyQueueForActor();

        await _aggregatedMeasureDataRequestDsl.InvalidRequestMessageAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_response_from_aggregated_measure_data_request()
    {
        await _notifyAggregatedMeasureDataResultDsl.EmptyQueueForActor();
        var gridArea = "804";
        var processId = await _aggregatedMeasureDataRequestDsl.InitializeAggregatedMeasureDataRequestAsync(
            gridArea,
            AcceptanceTestFixture.EdiSubsystemTestCimActorNumber,
            CancellationToken.None);

        await _notifyAggregatedMeasureDataResultDsl.PublishAggregatedMeasureDataRequestAcceptedResponseFor(
            processId,
            gridArea,
            CancellationToken.None);

        await _notifyAggregatedMeasureDataResultDsl.ConfirmResultIsAvailableFor();
    }

    // [Fact]
    // public async Task Actor_can_peek_and_dequeue_rejected_response_from_aggregated_measure_data_request()
    // {
    //     await _notifyWholesaleServicesDsl.EmptyQueueForActor();
    //     var gridArea = "888";
    //     var processId = await _wholesaleServicesRequestDsl.InitializeWholesaleServicesRequestAsync(
    //         gridArea,
    //         AcceptanceTestFixture.EdiSubsystemTestCimActorNumber,
    //         CancellationToken.None);
    //
    //     await _notifyWholesaleServicesDsl.PublishRejectedWholesaleServicesRequestAcceptedResponseFor(
    //         processId,
    //         gridArea,
    //         AcceptanceTestFixture.EdiSubsystemTestCimActorNumber,
    //         AcceptanceTestFixture.ActorNumber,
    //         CancellationToken.None);
    //
    //     await _notifyWholesaleServicesDsl.ConfirmResultIsAvailableFor();
    // }
}
