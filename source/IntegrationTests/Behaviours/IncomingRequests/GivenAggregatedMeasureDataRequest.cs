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

using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.Edi.Requests;
using FluentAssertions;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public class GivenAggregatedMeasureDataRequest : BehavioursTestBase
{
    public GivenAggregatedMeasureDataRequest(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
    }

    [Fact]
    public async Task InsertBetterTestNameHereAsync()
    {
        // Arrange
        var senderSpy = GivenServiceBusSenderSpy("Fake");
        GivenNowIs(2024, 7, 1);
        GivenAuthenticatedActorIs(ActorNumber.Create("2111111111111"), ActorRole.EnergySupplier);

        await GivenDelegationAsync(
            new ActorNumberAndRoleDto(ActorNumber.Create("2111111111111"), ActorRole.EnergySupplier),
            new ActorNumberAndRoleDto(ActorNumber.Create("1111111111111"), ActorRole.Delegated),
            "512",
            ProcessType.RequestEnergyResults,
            GetNow().Minus(Duration.FromDays(256)),
            GetNow().Plus(Duration.FromDays(256)));

        var responseMessage = await GivenRequestAggregatedMeasureDataJsonAsync(
            "2111111111111",
            ActorRole.EnergySupplier.Code,
            (2024, 5, 1),
            (2024, 6, 1),
            "512",
            "2111111111111");

        responseMessage.IsErrorResponse.Should().BeFalse(responseMessage.MessageBody);

        // Act
        senderSpy.Message.Should().NotBeNull();
        await WhenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(senderSpy.Message!);

        // Assert
        var message = senderSpy.Message;
        message.Should().NotBeNull();
        message!.Subject.Should().Be(nameof(AggregatedTimeSeriesRequest));
        message.Body.Should().NotBeNull();

        var aggregatedTimeSeriesRequest =
            AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body);

        aggregatedTimeSeriesRequest.Should().NotBeNull();
        aggregatedTimeSeriesRequest.GridAreaCode.Should().Be("512");
        aggregatedTimeSeriesRequest.RequestedByActorId.Should().Be("2111111111111");
        aggregatedTimeSeriesRequest.RequestedByActorRole.Should().Be("EnergySupplier");
        aggregatedTimeSeriesRequest.EnergySupplierId.Should().Be("2111111111111");
    }
}
