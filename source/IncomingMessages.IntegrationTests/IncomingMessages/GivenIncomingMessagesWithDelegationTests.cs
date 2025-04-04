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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.ProcessManager.Abstractions.Client;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.IncomingMessages;

public sealed class GivenIncomingMessagesWithDelegationTests : IncomingMessagesTestBase
{
    private readonly ClockStub _clockStub;
    private readonly Actor _originalActor = new(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
    private readonly Actor _delegatedTo = new(ActorNumber.Create("2222222222222"), ActorRole.Delegated);
    private readonly AuthenticatedActor _authenticatedActor;

    public GivenIncomingMessagesWithDelegationTests(
        IncomingMessagesTestFixture incomingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(incomingMessagesTestFixture, testOutputHelper)
    {
        _clockStub = (ClockStub)GetService<IClock>();
        _authenticatedActor = GetService<AuthenticatedActor>();
    }

    [Fact]
    public async Task AndGiven_MessageFromDelegated_When_Received_Then_ActorPropertiesOnInternalRepresentationAreCorrect()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy(StartSenderClientNames.ProcessManagerStartSender);
        var sut = GetService<IIncomingMessageClient>();

        const string gridAreaCode = "512";

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var documentFormat = DocumentFormat.Json;

        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole, null, ActorId));

        var messageStream = RequestAggregatedMeasureDataRequestBuilder.CreateIncomingMessage(
            DocumentFormat.Json,
            _delegatedTo.ActorNumber,
            _originalActor.ActorRole,
            null,
            null,
            Instant.FromUtc(2024, 01, 01, 0, 0),
            Instant.FromUtc(2024, 01, 31, 0, 0),
            _originalActor.ActorNumber,
            null,
            [
                (gridAreaCode, TransactionId.From("555555555555555555555555555555555555")),
            ]);

        await AddDelegationAsync(
            _originalActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            now,
            now.Plus(Duration.FromSeconds(1)));

        // Act
        var response = await sut.ReceiveIncomingMarketMessageAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.RequestAggregatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using (new AssertionScope())
        {
            response.IsErrorResponse.Should().BeFalse();
            response.MessageBody.Should().BeNullOrEmpty();

            senderSpy.LatestMessage.Should().NotBeNull();
        }

        using (new AssertionScope())
        {
            var requestInput = GetRequestCalculatedEnergyTimeSeriesInputV1(senderSpy);
            response.IsErrorResponse.Should().BeFalse();
            requestInput.RequestedByActorRole.Should().Be(_delegatedTo.ActorRole.Name);
            requestInput.RequestedByActorNumber.Should().Be(_delegatedTo.ActorNumber.Value);
            requestInput.RequestedForActorRole.Should().Be(_originalActor.ActorRole.Name);
            requestInput.RequestedForActorNumber.Should().Be(_originalActor.ActorNumber.Value);
            requestInput.EnergySupplierNumber.Should().Be(_originalActor.ActorNumber.Value);
            requestInput.GridAreas.Should().Contain(gridAreaCode);
        }
    }

    [Fact]
    public async Task AndGiven_MessageFromDelegated_AndGiven_DelegationHasStopped_When_Received_Then_ErrorResponseToActor()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy(StartSenderClientNames.ProcessManagerStartSender);
        var sut = GetService<IIncomingMessageClient>();

        const string gridAreaCode = "512";

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var documentFormat = DocumentFormat.Json;

        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole, null, ActorId));

        var messageStream = RequestAggregatedMeasureDataRequestBuilder.CreateIncomingMessage(
            DocumentFormat.Json,
            _delegatedTo.ActorNumber,
            _originalActor.ActorRole,
            null,
            null,
            Instant.FromUtc(2024, 01, 01, 0, 0),
            Instant.FromUtc(2024, 01, 31, 0, 0),
            _originalActor.ActorNumber,
            null,
            [
                (gridAreaCode, TransactionId.From("555555555555555555555555555555555555")),
            ],
            true);

        await AddDelegationAsync(
            _originalActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            now.Minus(Duration.FromMinutes(10)),
            now.Plus(Duration.FromMinutes(10)),
            1);

        // Cancel a delegation by adding a newer (higher sequence number) delegation to same receiver, with startsAt == stopsAt
        await AddDelegationAsync(
            _originalActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            now,
            now,
            2);

        // Act
        var response = await sut.ReceiveIncomingMarketMessageAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.RequestAggregatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using var scope = new AssertionScope();
        response.IsErrorResponse.Should().BeTrue();
        response.MessageBody.Should().Contain("The authenticated user does not hold the required role");
        senderSpy.LatestMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("1111111111111", null, "EnergySupplier")]
    [InlineData(null, "1111111111111", "BalanceResponsibleParty")]
    public async Task AndGiven_RequestMessageActorIsNotSameAsDelegatedBy_When_Received_Then_ReturnsErrorMessage(
        string? requestDataFromEnergySupplierId,
        string? requestDateFromBalanceResponsible,
        string requesterActorRole)
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy(StartSenderClientNames.ProcessManagerStartSender);
        var sut = GetService<IIncomingMessageClient>();

        const string gridAreaCode = "512";

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var documentFormat = DocumentFormat.Json;

        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole, null, ActorId));

        var messageStream = RequestAggregatedMeasureDataRequestBuilder.CreateIncomingMessage(
            DocumentFormat.Json,
            _delegatedTo.ActorNumber,
            ActorRole.FromName(requesterActorRole),
            null,
            null,
            Instant.FromUtc(2024, 01, 01, 0, 0),
            Instant.FromUtc(2024, 01, 31, 0, 0),
            requestDataFromEnergySupplierId != null ? ActorNumber.Create(requestDataFromEnergySupplierId) : null,
            requestDateFromBalanceResponsible != null ? ActorNumber.Create(requestDateFromBalanceResponsible) : null,
            [
                (gridAreaCode, TransactionId.From("555555555555555555555555555555555555")),
            ]);

        // Delegation by another EnergySupplier or BalanceResponsibleParty
        // on the same grid area as the request
        var delegatedByAnotherActorThenRequestedActor = new Actor(
            ActorNumber.Create("3333333333333"),
            ActorRole.FromName(requesterActorRole));

        await AddDelegationAsync(
            delegatedByAnotherActorThenRequestedActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            now,
            now.Plus(Duration.FromSeconds(1)));

        // Act
        var response = await sut.ReceiveIncomingMarketMessageAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.RequestAggregatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using var scope = new AssertionScope();
        response.IsErrorResponse.Should().BeTrue();
        response.MessageBody.Should().Contain("The authenticated user does not hold the required role");
        senderSpy.LatestMessage.Should()
            .BeNull(
                "Since there does not exist a delegation from the energySupplier/balanceResponsible on the requested grid area");
    }

    [Theory]
    [InlineData("1111111111111", null, "EnergySupplier")]
    [InlineData(null, "1111111111111", "BalanceResponsibleParty")]
    public async Task
        AndGiven_RequestMessageWithoutGridArea_AndGiven_AnotherDelegationExistByAnotherActor_When_Received_Then_ReceiveMessageForExpectedGridArea(
            string? requestDataForEnergySupplierId,
            string? requestDateForBalanceResponsible,
            string requesterActorRoleName)
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy(StartSenderClientNames.ProcessManagerStartSender);
        var sut = GetService<IIncomingMessageClient>();

        const string expectedGridAreaCode = "512";
        const string anotherGridAreaCode = "804";

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var documentFormat = DocumentFormat.Json;

        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole, null, ActorId));

        var energySupplier = requestDataForEnergySupplierId != null
            ? ActorNumber.Create(requestDataForEnergySupplierId)
            : null;

        var balanceResponsibleParty = requestDateForBalanceResponsible != null
            ? ActorNumber.Create(requestDateForBalanceResponsible)
            : null;

        var originalActorRole = ActorRole.FromName(requesterActorRoleName);

        var messageStream = RequestAggregatedMeasureDataRequestBuilder.CreateIncomingMessage(
            DocumentFormat.Json,
            _delegatedTo.ActorNumber,
            originalActorRole,
            null,
            null,
            Instant.FromUtc(2024, 01, 01, 0, 0),
            Instant.FromUtc(2024, 01, 31, 0, 0),
            energySupplier,
            balanceResponsibleParty,
            [
                (null, TransactionId.From("555555555555555555555555555555555555")),
            ]);

        await AddDelegationAsync(
            new Actor(
                originalActorRole == ActorRole.EnergySupplier ? energySupplier! : balanceResponsibleParty!,
                originalActorRole),
            _delegatedTo,
            expectedGridAreaCode,
            ProcessType.RequestEnergyResults,
            now,
            now.Plus(Duration.FromSeconds(1)));

        var delegatedByAnotherActorThenRequestedActor =
            new Actor(ActorNumber.Create("3333333333333"), originalActorRole);

        await AddDelegationAsync(
            delegatedByAnotherActorThenRequestedActor,
            _delegatedTo,
            anotherGridAreaCode,
            ProcessType.RequestEnergyResults,
            now,
            now.Plus(Duration.FromSeconds(1)));

        // Act
        var response = await sut.ReceiveIncomingMarketMessageAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.RequestAggregatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using (new AssertionScope())
        {
            response.IsErrorResponse.Should().BeFalse();
            response.MessageBody.Should().BeNullOrEmpty();

            senderSpy.LatestMessage.Should().NotBeNull();
        }

        using (new AssertionScope())
        {
            var requestInput = GetRequestCalculatedEnergyTimeSeriesInputV1(senderSpy);
            requestInput.RequestedByActorRole.Should().Be(_delegatedTo.ActorRole.Name);
            requestInput.RequestedByActorNumber.Should().Be(_delegatedTo.ActorNumber.Value);
            requestInput.RequestedForActorRole.Should().Be(originalActorRole.Name);
            requestInput.RequestedForActorNumber.Should().Be(_originalActor.ActorNumber.Value);
            requestInput.EnergySupplierNumber.Should().Be(energySupplier?.Value);
            requestInput.BalanceResponsibleNumber.Should().Be(balanceResponsibleParty?.Value);
            requestInput.GridAreas.Should().Equal(expectedGridAreaCode);
        }
    }

    [Fact]
    public async Task AndGiven_MessageIsMeteredDataForMeteringPoint_When_SenderIsDelegated_Then_MessageIsReceivedAndActorPropertiesOnInternalRepresentationAreCorrect()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy(StartSenderClientNames.Brs021ForwardMeteredDataStartSender);
        var sut = GetService<IIncomingMessageClient>();
        var transactionId = "555555555";
        var resolution = Resolution.QuarterHourly;

        var delegatedToAsDelegated = new Actor(ActorNumber.Create("2222222222222"), ActorRole.Delegated);
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var documentFormat = DocumentFormat.Ebix;

        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(delegatedToAsDelegated.ActorNumber, Restriction.Owned, delegatedToAsDelegated.ActorRole, null, ActorId));

        var messageStream = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
            documentFormat,
            delegatedToAsDelegated.ActorNumber,
            [
                (transactionId, Instant.FromUtc(2024, 01, 01, 0, 0), Instant.FromUtc(2024, 01, 31, 0, 0), resolution),
            ]);

        // Act
        var response = await sut.ReceiveIncomingMarketMessageAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.NotifyValidatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using (new AssertionScope())
        {
            response.IsErrorResponse.Should().BeFalse();
            senderSpy.LatestMessage.Should().NotBeNull();

            var startOrchestrationInstance = GetStartOrchestrationInstanceV1(senderSpy);
            var serializer = new Serializer();
            var requestInput = serializer.Deserialize<ForwardMeteredDataInputV1>(startOrchestrationInstance.Input);
            startOrchestrationInstance.StartedByActor.ActorRole.Should().Be(ConvertToActorRoleV1(delegatedToAsDelegated.ActorRole));
            startOrchestrationInstance.StartedByActor.ActorNumber.Should().Be(delegatedToAsDelegated.ActorNumber.Value);
            requestInput.ActorNumber.Should().Be(delegatedToAsDelegated.ActorNumber.Value);
            requestInput.ActorRole.Should().Be(ActorRole.MeteredDataResponsible.Name);
            requestInput.TransactionId.Should().Be(transactionId);
            requestInput.Resolution.Should().Be(resolution.Name);
        }
    }

    private static ActorRoleV1 ConvertToActorRoleV1(ActorRole actorRole)
    {
        return actorRole.Name switch
        {
            "Delegated" => ActorRoleV1.Delegated,
            _ => throw new ArgumentException($"Unknown actor role: {actorRole.Name}"),
        };
    }

    private static RequestCalculatedEnergyTimeSeriesInputV1 GetRequestCalculatedEnergyTimeSeriesInputV1(
        ServiceBusSenderSpy senderSpy)
    {
        var serializer = new Serializer();
        var parser = new MessageParser<StartOrchestrationInstanceV1>(() => new StartOrchestrationInstanceV1());
        var message = parser.ParseJson(senderSpy.LatestMessage!.Body.ToString());
        var input = serializer.Deserialize<RequestCalculatedEnergyTimeSeriesInputV1>(message.Input);
        return input;
    }

    private static StartOrchestrationInstanceV1 GetStartOrchestrationInstanceV1(
        ServiceBusSenderSpy senderSpy)
    {
        var parser = new MessageParser<StartOrchestrationInstanceV1>(() => new StartOrchestrationInstanceV1());
        var message = parser.ParseJson(senderSpy.LatestMessage!.Body.ToString());
        return message;
    }

    private async Task AddDelegationAsync(
        Actor delegatedBy,
        Actor delegatedTo,
        string gridAreaCode,
        ProcessType processType,
        Instant? startsAt = null,
        Instant? stopsAt = null,
        int sequenceNumber = 0)
    {
        var masterDataClient = GetService<IMasterDataClient>();
        await masterDataClient.CreateProcessDelegationAsync(
            new ProcessDelegationDto(
                sequenceNumber,
                processType,
                gridAreaCode,
                startsAt ?? SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
                stopsAt ?? SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)),
                delegatedBy,
                delegatedTo),
            CancellationToken.None);
    }
}
