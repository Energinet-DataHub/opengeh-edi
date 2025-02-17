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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C;
using Energinet.DataHub.EDI.SubsystemTests.Dsl;
using Energinet.DataHub.EDI.SubsystemTests.TestOrdering;
using FluentAssertions;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests;

[SuppressMessage(
    "Usage",
    "CA2007",
    Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
[TestCaseOrderer(
    ordererTypeName: "Energinet.DataHub.EDI.SubsystemTests.TestOrdering.TestOrderer",
    ordererAssemblyName: "Energinet.DataHub.Wholesale.SubsystemTests")]
[IntegrationTest]
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]

// TODO: Rename this to brs028 when we have deleted the old request tests
#pragma warning disable xUnit1000 // Skipping the tests in this class, since it's internal
internal sealed class WhenWholesaleSettlementRequestedProcessManagerTests : BaseTestClass
{
    private readonly NotifyWholesaleServicesDsl _notifyWholesaleServices;
    private readonly WholesaleSettlementRequestDsl _wholesaleSettlementRequest;

    public WhenWholesaleSettlementRequestedProcessManagerTests(SubsystemTestFixture fixture, ITestOutputHelper output)
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
                wholesaleDriver,
                new ProcessManagerDriver(fixture.EdiTopicClient));
    }

    [Fact]
    [Order(100)] // Default is 0, hence we assign this a higher number => it will run last, and therefor not interfere with the other tests
    public async Task B2B_actor_can_request_wholesale_settlement()
    {
        var act = async () => await _wholesaleSettlementRequest.Request(CancellationToken.None);

        await act.Should().NotThrowAsync("because the request should be valid");
    }

    [Fact]
    [Order(100)] // Default is 0, hence we assign this a higher number => it will run last, and therefor not interfere with the other tests
    public async Task B2C_actor_can_request_wholesale_settlement()
    {
        var act = async () => await _wholesaleSettlementRequest.B2CRequest(CancellationToken.None);

        await act.Should().NotThrowAsync("because the request should be valid");
    }

    [Fact]
    public async Task Actor_get_sync_rejected_response_when_wholesale_settlement_request_is_invalid()
    {
        await _wholesaleSettlementRequest.ConfirmInvalidRequestIsRejected(CancellationToken.None);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_response_from_wholesale_settlement_request()
    {
        await _wholesaleSettlementRequest.PublishAcceptedBrs028RequestAsync(
            "804",
            new Actor(
                ActorNumber.Create(SubsystemTestFixture.EZTestCimActorNumber),
                ActorRole.SystemOperator));

        await _notifyWholesaleServices.ConfirmResultIsAvailable();
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_rejected_response_from_wholesale_settlement_request()
    {
        await _wholesaleSettlementRequest.PublishRejectedBrs028RequestAsync(
            new Actor(
                ActorNumber.Create(SubsystemTestFixture.EZTestCimActorNumber),
                ActorRole.SystemOperator));

        await _notifyWholesaleServices.ConfirmRejectResultIsAvailable();
    }
}
