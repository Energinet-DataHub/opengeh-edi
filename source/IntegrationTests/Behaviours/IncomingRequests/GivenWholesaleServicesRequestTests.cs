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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.Edi.Requests;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test class")]
public class GivenWholesaleServicesRequestTests : BehavioursTestBase
{
    public GivenWholesaleServicesRequestTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
    }

    [Fact]
    public async Task
        Given_DelegationInTwoGridAreas_When_WholesaleServicesProcessIsInitialized_Then_WholesaleServiceBusMessageIsCorrect()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy("Fake");
        GivenNowIs(2024, 7, 1);
        var delegatedByActor = (ActorNumber: ActorNumber.Create("2111111111111"), ActorRole: ActorRole.EnergySupplier);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.Delegated);
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegationAsync(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenDelegationAsync(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenRequestWholesaleServicesAsync(
            DocumentFormat.Json,
            delegatedToActor.ActorNumber.Value,
            delegatedByActor.ActorRole.Code,
            (2024, 1, 1),
            (2024, 2, 1),
            null,
            delegatedByActor.ActorNumber.Value,
            "123564789123564789123564789123564787");

        // Act
        await WhenWholesaleServicesProcessIsInitializedAsync(senderSpy.Message!);

        // Assert
        await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512", "609" },
            requestedForActorNumber: "2111111111111",
            requestedForActorRole: "EnergySupplier",
            energySupplierId: "2111111111111");
    }

    private Task ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
        ServiceBusSenderSpy senderSpy,
        IReadOnlyCollection<string> gridAreas,
        string requestedForActorNumber,
        string requestedForActorRole,
        string energySupplierId)
    {
        using (new AssertionScope())
        {
            senderSpy.MessageSent.Should().BeTrue();
            senderSpy.Message.Should().NotBeNull();
        }

        var serviceBusMessage = senderSpy.Message!;
        using (new AssertionScope())
        {
            serviceBusMessage.Subject.Should().Be(nameof(WholesaleServicesRequest));
            serviceBusMessage.Body.Should().NotBeNull();
        }

        var wholesaleServicesRequestMessage = WholesaleServicesRequest.Parser.ParseFrom(serviceBusMessage.Body);
        wholesaleServicesRequestMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();
        wholesaleServicesRequestMessage.GridAreaCodes.Should().BeEquivalentTo(gridAreas);
        wholesaleServicesRequestMessage.RequestedForActorNumber.Should().Be(requestedForActorNumber);
        wholesaleServicesRequestMessage.RequestedForActorRole.Should().Be(requestedForActorRole);
        wholesaleServicesRequestMessage.EnergySupplierId.Should().Be(energySupplierId);

        return Task.CompletedTask;
    }
}
