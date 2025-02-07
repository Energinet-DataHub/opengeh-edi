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
[SuppressMessage(
    "Usage",
    "xUnit1000",
    Justification = "By making it abstract, we avoid running the tests in this class")] // TODO: Remove this when we are ready to enqueue brs026 messages
[IntegrationTest]
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
// public sealed class WhenEnergyResultRequestedProcessManagerTests : BaseTestClass
internal abstract class WhenEnergyResultRequestedProcessManagerTests : BaseTestClass
{
    private readonly NotifyAggregatedMeasureDataResultDsl _notifyAggregatedMeasureDataResult;
    private readonly AggregatedMeasureDataRequestDsl _aggregatedMeasureDataRequest;

    public WhenEnergyResultRequestedProcessManagerTests(SubsystemTestFixture fixture, ITestOutputHelper output)
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
                wholesaleDriver,
                new ProcessManagerDriver(fixture.EdiTopicClient));
    }

    [Fact]
    public async Task Given_GoodRequest_When_B2BActorRequests_Then_GetSuccessfulResponse()
    {
        var act = async () => await _aggregatedMeasureDataRequest.Request(CancellationToken.None);

        await act.Should().NotThrowAsync("because the request should be valid");
    }

    [Fact]
    public async Task Given_GoodRequest_When_B2CActorRequests_Then_GetSuccessfulResponse()
    {
        var act = async () => await _aggregatedMeasureDataRequest.B2CRequest(CancellationToken.None);

        await act.Should().NotThrowAsync("because the request should be valid");
    }

    [Fact]
    public async Task Given_BadRequest_When_ActorRequests_Then_GetBadRequest()
    {
        await _aggregatedMeasureDataRequest.ConfirmInvalidRequestIsRejected(CancellationToken.None);
    }

    [Fact]
    public async Task Given_EnqueueBrs026FromProcessManager_When_ActorPeeks_Then_GetsNotifyMessage()
    {
        await _aggregatedMeasureDataRequest.PublishAcceptedRequestBrs026Async(
            "804",
            new Actor(ActorNumber.Create(SubsystemTestFixture.EdiSubsystemTestCimEnergySupplierNumber), ActorRole.EnergySupplier));

        await _notifyAggregatedMeasureDataResult.ConfirmResultIsAvailable();
    }

    [Fact]
    public async Task Given_EnqueueRejectBrs026FromProcessManager_When_ActorPeeks_Then_ActorGetRejectedMessage()
    {
        await _aggregatedMeasureDataRequest.PublishRejectedRequestBrs026Async(
            new Actor(ActorNumber.Create(SubsystemTestFixture.EdiSubsystemTestCimEnergySupplierNumber), ActorRole.EnergySupplier));

        await _notifyAggregatedMeasureDataResult.ConfirmRejectResultIsAvailable();
    }
}
