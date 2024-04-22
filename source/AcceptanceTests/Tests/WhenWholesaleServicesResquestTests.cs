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
public sealed class WhenWholesaleServicesResquestTests
{
    private readonly NotifyWholesaleServicesDsl _notifyWholesaleServicesDsl;
    private readonly WholesaleServicesRequestDsl _wholesaleServicesRequestDsl;

    public WhenWholesaleServicesResquestTests(AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var ediDriver = new EdiDriver(fixture.B2BEnergySupplierAuthorizedHttpClient);
        var ediProcessesDriver = new EdiProcessesDriver(fixture.ConnectionString);
        _notifyWholesaleServicesDsl = new NotifyWholesaleServicesDsl(
            ediDriver,
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient));

        _wholesaleServicesRequestDsl = new WholesaleServicesRequestDsl(ediProcessesDriver, ediDriver);
    }

    [Fact]
    public async Task Actor_can_request_wholesale_services()
    {
        await _notifyWholesaleServicesDsl.EmptyQueueForActor();

        var messageId = await _wholesaleServicesRequestDsl.RequestAsync(CancellationToken.None);

        await _wholesaleServicesRequestDsl.ConfirmRequestIsInitiatedAsync(
            messageId,
            CancellationToken.None);
    }

    // [Fact]
    // public async Task Actor_request_invalid_wholesale_services()
    // {
    //     await _notifyWholesaleServicesDsl.EmptyQueueForActor();
    //
    //     var messageId = await _wholesaleServicesRequestDsl.RequestAsync(CancellationToken.None);
    //
    //     await _wholesaleServicesRequestDsl.ConfirmRequestIsRejectedAsync(
    //         messageId,
    //         CancellationToken.None);
    // }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_response_from_wholesale_services_request()
    {
        await _notifyWholesaleServicesDsl.EmptyQueueForActor();
        var gridArea = "888";
        var processId = await _wholesaleServicesRequestDsl.InitializeWholesaleServicesRequestAsync(
            gridArea,
            AcceptanceTestFixture.EdiSubsystemTestCimActorNumber,
            CancellationToken.None);

        await _notifyWholesaleServicesDsl.PublishWholesaleServicesRequestAcceptedResponseFor(
            processId,
            gridArea,
            AcceptanceTestFixture.EdiSubsystemTestCimActorNumber,
            AcceptanceTestFixture.ActorNumber,
            CancellationToken.None);

        await _notifyWholesaleServicesDsl.ConfirmResultIsAvailableFor();
    }

    // [Fact]
    // public async Task Actor_can_peek_and_dequeue_rejected_response_from_wholesale_services_request()
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
