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
public sealed class WhenWholesaleSettlementRequestedTests
{
    private readonly NotifyWholesaleServicesDsl _notifyWholesaleServices;
    private readonly WholesaleSettlementRequestDsl _wholesaleSettlementRequest;

    public WhenWholesaleSettlementRequestedTests(AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var ediDriver = new EdiDriver(fixture.B2BSystemOperatorAuthorizedHttpClient);
        var ediProcessesDriver = new EdiProcessesDriver(fixture.ConnectionString);
        var wholesaleDriver = new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient);

        _notifyWholesaleServices = new NotifyWholesaleServicesDsl(
            ediDriver,
            wholesaleDriver);

        _wholesaleSettlementRequest =
            new WholesaleSettlementRequestDsl(ediProcessesDriver, ediDriver, wholesaleDriver);
    }

    [Fact]
    public async Task Actor_can_request_wholesale_settlement()
    {
        await _notifyWholesaleServices.EmptyQueueForActor();

        var messageId = await _wholesaleSettlementRequest.RequestAsync(CancellationToken.None);

        await _wholesaleSettlementRequest.ConfirmRequestIsInitializedAsync(
            messageId,
            CancellationToken.None);
    }

    [Fact]
    public async Task Actor_get_bad_request_when_wholesale_settlement_request_is_invalid()
    {
        await _notifyWholesaleServices.EmptyQueueForActor();

        await _wholesaleSettlementRequest.ConfirmInvalidRequestIsRejectedAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_response_from_wholesale_settlement_request()
    {
        await _notifyWholesaleServices.EmptyQueueForActor();
        var gridAreaCode = "888";
        var processId = await _wholesaleSettlementRequest.InitializeWholesaleSettlementRequestAsync(
            gridAreaCode,
            AcceptanceTestFixture.EZTestCimActorNumber,
            CancellationToken.None);

        await _wholesaleSettlementRequest.PublishWholesaleServicesRequestAcceptedResponseAsync(
            processId,
            gridAreaCode,
            AcceptanceTestFixture.ActorNumber,
            AcceptanceTestFixture.EZTestCimActorNumber,
            CancellationToken.None);

        await _notifyWholesaleServices.ConfirmResultIsAvailable();
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_rejected_response_from_wholesale_settlement_request()
    {
        await _notifyWholesaleServices.EmptyQueueForActor();
        var gridAreaCode = "888";
        var processId = await _wholesaleSettlementRequest.InitializeWholesaleSettlementRequestAsync(
            gridAreaCode,
            AcceptanceTestFixture.EZTestCimActorNumber,
            CancellationToken.None);

        await _wholesaleSettlementRequest.PublishWholesaleServicesRequestRejectedResponseAsync(
            processId,
            CancellationToken.None);

        await _notifyWholesaleServices.ConfirmRejectResultIsAvailable();
    }
}
