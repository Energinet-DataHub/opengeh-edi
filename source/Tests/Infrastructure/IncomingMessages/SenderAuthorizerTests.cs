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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.IncomingMessages;

public class SenderAuthorizerTests
{
    /*
     * Yes, this test class is a bit empty. Most cases are handled by the rather bloated IncomingMessageReceiverTests.
     * We should probably do something about that. Eventually. Maybe.
     */

    private readonly Guid _actorId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task
        Given_UnauthorisedUser_When_ValidatingSenderForAggregatedMeasureData_Then_SenderRoleTypeIsNotAuthorizedError()
    {
        var authenticatedActor = CreateAuthenticatedActor(
            ActorNumber.Create("1213141516178"),
            ActorRole.SystemOperator);
        var sut = CreateSut(authenticatedActor);

        var incomingMessage = new RequestAggregatedMeasureDataMessage(
            "1213141516178",
            ActorRole.SystemOperator.Code,
            DataHubDetails.DataHubActorNumber.Value,
            ActorRole.MeteredDataAdministrator.Code,
            "BusinessReason",
            "MessageType",
            "MessageId",
            "CreatedAt",
            "BusinessType",
            new List<IIncomingMessageSeries>());

        var result = await sut.AuthorizeAsync(incomingMessage, false);

        result.Errors.Should().ContainSingle().Which.Should().BeAssignableTo(typeof(SenderRoleTypeIsNotAuthorized));
    }

    [Fact]
    public async Task
        Given_UnauthorisedUser_When_ValidatingSenderForWholesaleServices_Then_SenderRoleTypeIsNotAuthorizedError()
    {
        var authenticatedActor = CreateAuthenticatedActor(
            ActorNumber.Create("1213141516178"),
            ActorRole.BalanceResponsibleParty);
        var sut = CreateSut(authenticatedActor);

        var incomingMessage = new RequestWholesaleServicesMessage(
            "1213141516178",
            ActorRole.BalanceResponsibleParty.Code,
            DataHubDetails.DataHubActorNumber.Value,
            ActorRole.MeteredDataAdministrator.Code,
            "BusinessReason",
            "MessageType",
            "MessageId",
            "CreatedAt",
            "BusinessType",
            new List<IIncomingMessageSeries>());

        var result = await sut.AuthorizeAsync(incomingMessage, false);

        result.Errors.Should().ContainSingle().Which.Should().BeAssignableTo(typeof(SenderRoleTypeIsNotAuthorized));
    }

    [Fact]
    public async Task
        Given_ActorIsNotMeteredDataResponsible_When_MeteredDataForMeasurementPoint_Then_SenderRoleTypeIsNotAuthorizedError()
    {
        var senderRole = ActorRole.BalanceResponsibleParty;
        var authenticatedActor = CreateAuthenticatedActor(
            ActorNumber.Create("1213141516178"),
            senderRole);
        var sut = CreateSut(authenticatedActor);

        var incomingMessage = new MeteredDataForMeasurementPointMessageBase(
            "MessageId",
            "MessageType",
            "CreatedAt",
            "1213141516178",
            DataHubDetails.DataHubActorNumber.Value,
            senderRole.Code,
            "BusinessReason",
            string.Empty,
            "BusinessType",
            new List<IIncomingMessageSeries>());

        var result = await sut.AuthorizeAsync(incomingMessage, false);

        result.Errors.Should().ContainSingle().Which.Should().BeAssignableTo(typeof(SenderRoleTypeIsNotAuthorized));
    }

    [Fact]
    public async Task
        Given_ActorIsMeteredDataResponsible_When_MeteredDataForMeasurementPoint_Then_SenderIsAuthorized()
    {
        var senderRole = ActorRole.MeteredDataResponsible;
        var authenticatedActor = CreateAuthenticatedActor(
            ActorNumber.Create("1213141516178"),
            senderRole);
        var sut = CreateSut(authenticatedActor);

        var incomingMessage = new MeteredDataForMeasurementPointMessageBase(
            "MessageId",
            "MessageType",
            "CreatedAt",
            "1213141516178",
            DataHubDetails.DataHubActorNumber.Value,
            senderRole.Code,
            "BusinessReason",
            string.Empty,
            "BusinessType",
            new List<IIncomingMessageSeries>());

        var result = await sut.AuthorizeAsync(incomingMessage, false);
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    private AuthenticatedActor CreateAuthenticatedActor(ActorNumber actorNumber, ActorRole actorRole)
    {
        var actorIdentity = new ActorIdentity(actorNumber, Restriction.Owned, actorRole, _actorId);
        var authenticatedActor = new AuthenticatedActor();
        authenticatedActor.SetAuthenticatedActor(actorIdentity);

        return authenticatedActor;
    }

    private SenderAuthorizer CreateSut(AuthenticatedActor authenticatedActor)
    {
        return new SenderAuthorizer(authenticatedActor);
    }
}
