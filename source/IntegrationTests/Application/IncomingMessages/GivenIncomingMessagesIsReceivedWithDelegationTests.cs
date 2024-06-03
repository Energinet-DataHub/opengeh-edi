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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Interfaces;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;

public class GivenIncomingMessagesIsReceivedWithDelegationTests : TestBase
{
    private readonly SystemDateTimeProviderStub _dateTimeProvider;
    private readonly IIncomingMessageClient _incomingMessagesRequest;

    private readonly Actor _originalActor = new(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
    private readonly Actor _delegatedTo = new(ActorNumber.Create("2222222222222"), ActorRole.Delegated);
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly AuthenticatedActor _authenticatedActor;

    public GivenIncomingMessagesIsReceivedWithDelegationTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
        _incomingMessagesRequest = GetService<IIncomingMessageClient>();
        _dateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
        _authenticatedActor = GetService<AuthenticatedActor>();
    }

    [Fact]
    public async Task Receive_message_from_delegated()
    {
        // Arrange
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _dateTimeProvider.SetNow(now);
        var gridAreaCode = "512";
        var documentFormat = DocumentFormat.Json;
        _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

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
            new List<(string? GridArea, TransactionId TransactionId)>
            {
                (gridAreaCode, TransactionId.From("555555555555555555555555555555555555")),
            });

        await AddDelegationAsync(
            _originalActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            startsAt: now,
            stopsAt: now.Plus(Duration.FromSeconds(1)));

        // Act
        var response = await _incomingMessagesRequest.RegisterAndSendAsync(
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
    public async Task Receive_message_from_delegated_when_delegation_has_stopped()
    {
        // Arrange
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _dateTimeProvider.SetNow(now);
        var gridAreaCode = "512";
        var documentFormat = DocumentFormat.Json;
        _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

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
            new List<(string? GridArea, TransactionId TransactionId)>
            {
                (gridAreaCode, TransactionId.From("555555555555555555555555555555555555")),
            },
            true);

        await AddDelegationAsync(
            _originalActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            startsAt: now.Minus(Duration.FromMinutes(10)),
            stopsAt: now.Plus(Duration.FromMinutes(10)),
            sequenceNumber: 1);

        // Cancel a delegation by adding a newer (higher sequence number) delegation to same receiver, with startsAt == stopsAt
        await AddDelegationAsync(
            _originalActor,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            startsAt: now,
            stopsAt: now,
            sequenceNumber: 2);

        // Act
        var response = await _incomingMessagesRequest.RegisterAndSendAsync(
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
    public async Task GivenAnd_RequestMessageActorIsNotSameAsDelegatedBy_Then_ReturnsErrorMessage(string? requestDataFromEnergySupplierId, string? requestDateFromBalanceResponsible, string requesterActorRole)
    {
        // Arrange
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _dateTimeProvider.SetNow(now);
        var gridAreaCode = "512";
        var documentFormat = DocumentFormat.Json;
        _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

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
            new List<(string? GridArea, TransactionId TransactionId)>
            {
                (gridAreaCode, TransactionId.From("555555555555555555555555555555555555")),
            });

        var delegatedByAnotherActorThenActorRequested = new Actor(ActorNumber.Create("3333333333333"), ActorRole.FromName(requesterActorRole));
        // Delegation for another EnergySupplier or BalanceResponsibleParty
        // on the same grid area as the requested
        await AddDelegationAsync(
            delegatedByAnotherActorThenActorRequested,
            _delegatedTo,
            gridAreaCode,
            ProcessType.RequestEnergyResults,
            startsAt: now,
            stopsAt: now.Plus(Duration.FromSeconds(1)));

        // Act
        var response = await _incomingMessagesRequest.RegisterAndSendAsync(
            messageStream,
            documentFormat,
            IncomingDocumentType.RequestAggregatedMeasureData,
            documentFormat,
            CancellationToken.None);

        // Assert
        using var scope = new AssertionScope();
        response.IsErrorResponse.Should().BeTrue();
        response.MessageBody.Should().Contain("The authenticated user does not hold the required role");
        _senderSpy.LatestMessage.Should().BeNull("Since there does not exist a delegation from the energySupplier/balanceResponsible on the requested grid area");
    }

    [Theory]
    [InlineData("1111111111111", null, "EnergySupplier")]
    [InlineData(null, "1111111111111", "BalanceResponsibleParty")]
    public async Task GivenAnd_RequestMessageWithoutGridArea_WhenAnotherDelegationExistByAnotherActor_Then_ReceiveMessageForExpectedGridArea(string? requestDataForEnergySupplierId, string? requestDateForBalanceResponsible, string requesterActorRoleName)
    {
        // Arrange
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        _dateTimeProvider.SetNow(now);
        var expectedGridAreaCode = "512";
        var documentFormat = DocumentFormat.Json;
        _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(_delegatedTo.ActorNumber, Restriction.Owned, _delegatedTo.ActorRole));

        var energySupplier = requestDataForEnergySupplierId != null ? ActorNumber.Create(requestDataForEnergySupplierId) : null;
        var balanceResponsibleParty = requestDateForBalanceResponsible != null ? ActorNumber.Create(requestDateForBalanceResponsible) : null;
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
            new List<(string? GridArea, TransactionId TransactionId)>
            {
                (null, TransactionId.From("555555555555555555555555555555555555")),
            });

        await AddDelegationAsync(
            new Actor(originalActorRole == ActorRole.EnergySupplier ? energySupplier! : balanceResponsibleParty!, originalActorRole),
            _delegatedTo,
            expectedGridAreaCode,
            ProcessType.RequestEnergyResults,
            startsAt: now,
            stopsAt: now.Plus(Duration.FromSeconds(1)));

        var delegatedByAnotherActorThenActorRequested = new Actor(ActorNumber.Create("3333333333333"), originalActorRole);
        var anotherGridAreaCode = "804";
        await AddDelegationAsync(
            delegatedByAnotherActorThenActorRequested,
            _delegatedTo,
            anotherGridAreaCode,
            ProcessType.RequestEnergyResults,
            startsAt: now,
            stopsAt: now.Plus(Duration.FromSeconds(1)));

        // Act
        var response = await _incomingMessagesRequest.RegisterAndSendAsync(
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

    protected override void Dispose(bool disposing)
    {
        _serviceBusClientSenderFactory.Dispose();
        _senderSpy.Dispose();
        base.Dispose(disposing);
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
                processType ?? ProcessType.RequestEnergyResults,
                gridAreaCode,
                startsAt ?? SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
                stopsAt ?? SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)),
                delegatedBy,
                delegatedTo),
            CancellationToken.None);
    }
}
