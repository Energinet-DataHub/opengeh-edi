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
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Interfaces;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Azure;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.IncomingMessages;

public sealed class GivenIncomingMessagesWithDelegationTests : IncomingMessagesTestBase
{
    private readonly ClockStub _clockStub;
    private readonly IIncomingMessageClient _incomingMessagesRequest;

    private readonly Actor _originalActor = new(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
    private readonly Actor _delegatedTo = new(ActorNumber.Create("2222222222222"), ActorRole.Delegated);
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed in test")]
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly AuthenticatedActor _authenticatedActor;

    public GivenIncomingMessagesWithDelegationTests(
        IncomingMessagesTestFixture incomingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(incomingMessagesTestFixture, testOutputHelper)
    {
        _senderSpy = new ServiceBusSenderSpy("Fake");
        var serviceBusClientSenderFactory =
            (ServiceBusSenderFactoryStub)GetService<IAzureClientFactory<ServiceBusSender>>();

        serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
        _incomingMessagesRequest = GetService<IIncomingMessageClient>();
        _clockStub = (ClockStub)GetService<IClock>();
        _authenticatedActor = GetService<AuthenticatedActor>();
    }

    [Fact]
    public async Task AndGiven_MessageFromDelegated_When_Received_Then_ActorPropertiesOnInternalRepresentationAreCorrect()
    {
        // Arrange
        const string gridAreaCode = "512";

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var documentFormat = DocumentFormat.Json;

        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

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
        var response = await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
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

            _senderSpy.LatestMessage.Should().NotBeNull();
        }

        using (new AssertionScope())
        {
            var message = _senderSpy.LatestMessage!.Body.ToObjectFromJson<InitializeAggregatedMeasureDataProcessDto>();
            var series = message.Series.Should().ContainSingle().Subject;
            series.RequestedByActor.ActorRole.Should().Be(_delegatedTo.ActorRole);
            series.RequestedByActor.ActorNumber.Should().Be(_delegatedTo.ActorNumber);
            series.OriginalActor.ActorRole.Should().Be(_originalActor.ActorRole);
            series.OriginalActor.ActorNumber.Should().Be(_originalActor.ActorNumber);
            series.EnergySupplierNumber.Should().Be(_originalActor.ActorNumber.Value);
            series.RequestedGridAreaCode.Should().Be(gridAreaCode);
            series.GridAreas.Should().Equal(gridAreaCode);
        }
    }

    [Fact]
    public async Task AndGiven_MessageFromDelegated_AndGiven_DelegationHasStopped_When_Received_Then_ErrorResponseToActor()
    {
        // Arrange
        const string gridAreaCode = "512";

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var documentFormat = DocumentFormat.Json;

        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

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
        var response = await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.RequestAggregatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using var scope = new AssertionScope();
        response.IsErrorResponse.Should().BeTrue();
        response.MessageBody.Should().Contain("The authenticated user does not hold the required role");
        _senderSpy.LatestMessage.Should().BeNull();
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
        const string gridAreaCode = "512";

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var documentFormat = DocumentFormat.Json;

        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

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
        var response = await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.RequestAggregatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using var scope = new AssertionScope();
        response.IsErrorResponse.Should().BeTrue();
        response.MessageBody.Should().Contain("The authenticated user does not hold the required role");
        _senderSpy.LatestMessage.Should()
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
        const string expectedGridAreaCode = "512";
        const string anotherGridAreaCode = "804";

        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var documentFormat = DocumentFormat.Json;

        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

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
        var response = await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
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

            _senderSpy.LatestMessage.Should().NotBeNull();
        }

        using (new AssertionScope())
        {
            var message = _senderSpy.LatestMessage!.Body.ToObjectFromJson<InitializeAggregatedMeasureDataProcessDto>();
            var series = message.Series.Should().ContainSingle().Subject;
            series.RequestedByActor.ActorRole.Should().Be(_delegatedTo.ActorRole);
            series.RequestedByActor.ActorNumber.Should().Be(_delegatedTo.ActorNumber);
            series.OriginalActor.ActorRole.Should().Be(originalActorRole);
            series.OriginalActor.ActorNumber.Should().Be(_originalActor.ActorNumber);
            series.EnergySupplierNumber.Should().Be(energySupplier?.Value);
            series.BalanceResponsibleNumber.Should().Be(balanceResponsibleParty?.Value);
            series.RequestedGridAreaCode.Should().BeNull();
            series.GridAreas.Should().Equal(expectedGridAreaCode);
        }
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
