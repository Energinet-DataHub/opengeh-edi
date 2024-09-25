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
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.B2C;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using NodaTime;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

[SuppressMessage(
    "Usage",
    "CA2007",
    Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
[IntegrationTest]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public sealed class WhenWholesaleSettlementRequestedTests : BaseTestClass
{
    private readonly NotifyWholesaleServicesDsl _notifyWholesaleServices;
    private readonly WholesaleSettlementRequestDsl _wholesaleSettlementRequest;
    private readonly string _energySupplierActorNumber;

    public WhenWholesaleSettlementRequestedTests(AcceptanceTestFixture fixture, ITestOutputHelper output)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var ediDriver = new EdiDriver(fixture.DurableClient, fixture.B2BClients.SystemOperator, output);
        var wholesaleDriver = new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient);

        _notifyWholesaleServices = new NotifyWholesaleServicesDsl(
            ediDriver,
            wholesaleDriver);

        _wholesaleSettlementRequest =
            new WholesaleSettlementRequestDsl(
                new EdiDatabaseDriver(fixture.ConnectionString),
                ediDriver,
                new B2CEdiDriver(fixture.B2CClients.EnergySupplier, fixture.ApiManagementUri, fixture.EdiB2CWebApiUri, output),
                wholesaleDriver);

        _energySupplierActorNumber = AcceptanceTestFixture.EdiSubsystemTestCimEnergySupplierNumber;
    }

    [Fact]
    public async Task Actor_can_request_wholesale_settlement()
    {
        var messageId = await _wholesaleSettlementRequest.Request(CancellationToken.None);

        await _wholesaleSettlementRequest.ConfirmRequestIsInitialized(messageId);
    }

    [Fact]
    public async Task B2C_actor_can_request_wholesale_settlement()
    {
        var createdAfter = SystemClock.Instance.GetCurrentInstant();
        var energySupplierActorNumber = _energySupplierActorNumber;
        await _wholesaleSettlementRequest.B2CRequest(cancellationToken: CancellationToken.None);

        await _wholesaleSettlementRequest.ConfirmRequestIsInitialized(
            createdAfter,
            requestedByActorNumber: energySupplierActorNumber);
    }

    [Fact]
    public async Task Actor_get_sync_rejected_response_when_wholesale_settlement_request_is_invalid()
    {
        await _wholesaleSettlementRequest.ConfirmInvalidRequestIsRejected(CancellationToken.None);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_response_from_wholesale_settlement_request()
    {
        await _wholesaleSettlementRequest.PublishWholesaleServicesRequestAcceptedResponse(
            "888",
            AcceptanceTestFixture.ActorNumber,
            AcceptanceTestFixture.EZTestCimActorNumber,
            CancellationToken.None);

        await _notifyWholesaleServices.ConfirmResultIsAvailable();
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_rejected_response_from_wholesale_settlement_request()
    {
        await _wholesaleSettlementRequest.PublishWholesaleServicesRequestRejectedResponse(
            "888",
            AcceptanceTestFixture.EZTestCimActorNumber,
            CancellationToken.None);

        await _notifyWholesaleServices.ConfirmRejectResultIsAvailable();
    }
}
