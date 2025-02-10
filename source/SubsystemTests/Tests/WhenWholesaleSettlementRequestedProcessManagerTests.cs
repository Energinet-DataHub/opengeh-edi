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
using FluentAssertions;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests;

[SuppressMessage(
    "Usage",
    "CA2007",
    Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
[IntegrationTest]
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
// TODO: Rename this to brs028 when we have deleted the old request tests
public sealed class WhenWholesaleSettlementRequestedProcessManagerTests : BaseTestClass
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
    public async Task Given_GoodRequest_When_B2BActorRequests_Then_GetSuccessfulResponse()
    {
        var act = async () => await _wholesaleSettlementRequest.Request(CancellationToken.None);

        await act.Should().NotThrowAsync("because the request should be valid");
    }

    [Fact]
    public async Task Given_GoodRequest_When_B2CActorRequests_Then_GetSuccessfulResponse()
    {
        var act = async () => await _wholesaleSettlementRequest.B2CRequest(CancellationToken.None);

        await act.Should().NotThrowAsync("because the request should be valid");
    }

    [Fact]
    public async Task Given_BadRequest_When_ActorRequests_Then_GetBadRequest()
    {
        await _wholesaleSettlementRequest.ConfirmInvalidRequestIsRejected(CancellationToken.None);
    }

    [Fact]
    public async Task Given_EnqueueBrs028FromProcessManager_When_ActorPeeks_Then_GetsNotifyMessage()
    {
        await _wholesaleSettlementRequest.PublishAcceptedRequestBrs028Async(
            "804",
            new Actor(
                ActorNumber.Create(SubsystemTestFixture.EZTestCimActorNumber),
                ActorRole.SystemOperator));

        await _notifyWholesaleServices.ConfirmResultIsAvailable();
    }

    [Fact]
    public async Task Given_EnqueueRejectBrs028FromProcessManager_When_ActorPeeks_Then_ActorGetRejectedMessage()
    {
        await _wholesaleSettlementRequest.PublishRejectedRequestBrs026Async(
            new Actor(
                ActorNumber.Create(SubsystemTestFixture.EZTestCimActorNumber),
                ActorRole.SystemOperator));

        await _notifyWholesaleServices.ConfirmRejectResultIsAvailable();
    }
}
