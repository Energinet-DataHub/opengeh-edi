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

using System.Xml.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages;
using Energinet.DataHub.EDI.Tests.Factories;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages;

public class WhenEnqueueingOutgoingMessageWithDelegationTests : OutgoingMessagesTestBase
{
    private readonly EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder;
    private readonly AcceptedEnergyResultMessageDtoBuilder _acceptedEnergyResultMessageDtoBuilder;
    private readonly EnergyResultPerGridAreaMessageDtoBuilder _energyResultPerGridAreaMessageDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly ActorMessageQueueContext _context;
    private readonly ClockStub _clockStub;
    private readonly Actor _delegatedTo = CreateActor(ActorNumber.Create("1234567891235"), actorRole: ActorRole.Delegated);

    private Actor _delegatedBy = CreateActor(ActorNumber.Create("1234567891234"));

    public WhenEnqueueingOutgoingMessageWithDelegationTests(OutgoingMessagesTestFixture outgoingMessagesTestFixture, ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
        _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder = new EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        _context = GetService<ActorMessageQueueContext>();
        _clockStub = (ClockStub)GetService<IClock>();
        _acceptedEnergyResultMessageDtoBuilder = new AcceptedEnergyResultMessageDtoBuilder();
        _energyResultPerGridAreaMessageDtoBuilder = new EnergyResultPerGridAreaMessageDtoBuilder();
    }

    /// <summary>
    /// This is implemented to support the "hack" where
    ///     MeteredDataResponsible is working as GridOperator.
    /// </summary>
    [Fact]
    public async Task
        Given_DelegatedByIsGridOperator_When_EnqueuingOutgoingEnergyResultMessageToMeteredDataResponsible_Then_GridOperatorReceivesMessage()
    {
        // Arrange
        var outgoingEnergyResultMessageReceiver = CreateActor(ActorNumber.Create("1234567891234"), actorRole: ActorRole.MeteredDataResponsible);
        var message = _energyResultPerGridAreaMessageDtoBuilder
            .WithMeteredDataResponsibleNumber(outgoingEnergyResultMessageReceiver.ActorNumber.Value)
            .Build();

        _delegatedBy = CreateActor(outgoingEnergyResultMessageReceiver.ActorNumber, actorRole: ActorRole.GridAccessProvider);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode);

        // Act
        ClearDbContextCaches();
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedTo, outgoingEnergyResultMessageReceiver);
    }

    [Fact]
    public async Task Enqueue_message_to_delegated()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithBalanceResponsiblePartyReceiverNumber(_delegatedBy.ActorNumber.Value)
            .Build();

        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode);

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedTo, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_delegated_with_multiple_delegations()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithBalanceResponsiblePartyReceiverNumber(_delegatedBy.ActorNumber.Value)
            .Build();

        await AddMockDelegationsForActorAsync(_delegatedBy);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode);

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedTo, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_delegated_as_grid_operator()
    {
        // Arrange
        var delegatedBy = CreateActor(ActorNumber.Create("1234567891234"), actorRole: ActorRole.GridAccessProvider);
        var delegatedTo = CreateActor(ActorNumber.Create("1234567891235"), actorRole: ActorRole.GridAccessProvider);
        var builder = new WholesaleTotalAmountMessageDtoBuilder();
        var message = builder.WithReceiverNumber(delegatedBy.ActorNumber)
            .WithReceiverRole(ActorRole.GridAccessProvider).Build();
        await AddDelegationAsync(delegatedBy, delegatedTo, message.Series.GridAreaCode!, ProcessType.ReceiveWholesaleResults);

        // Act
        ClearDbContextCaches();
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);

        // Assert
        await AssertEnqueuedOutgoingMessage(delegatedTo, delegatedBy, DocumentType.NotifyWholesaleServices, BusinessReason.WholesaleFixing);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_when_delegation_has_expired()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithBalanceResponsiblePartyReceiverNumber(_delegatedBy.ActorNumber.Value)
            .Build();

        var endsAtInThePast = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(1));
        await AddDelegationAsync(
            _delegatedBy,
            _delegatedTo,
            message.SeriesForBalanceResponsible.GridAreaCode,
            startsAt: SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(10)),
            stopsAt: endsAtInThePast);

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedBy, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_when_delegation_is_in_future()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithBalanceResponsiblePartyReceiverNumber(_delegatedBy.ActorNumber.Value)
            .Build();

        var startsAtInTheFuture = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5));
        await AddDelegationAsync(
            _delegatedBy,
            _delegatedTo,
            message.SeriesForBalanceResponsible.GridAreaCode,
            startsAt: startsAtInTheFuture,
            stopsAt: SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(10)));

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedBy, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_when_delegation_has_stopped()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithBalanceResponsiblePartyReceiverNumber(_delegatedBy.ActorNumber.Value)
            .Build();

        var startsAt = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(10));
        var now = SystemClock.Instance.GetCurrentInstant();
        _clockStub.SetCurrentInstant(now);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode, startsAt: startsAt, stopsAt: now.Plus(Duration.FromDays(30)), sequenceNumber: 0);

        // Cancel a delegation by adding a newer (higher sequence number) delegation to same receiver, with startsAt == stopsAt
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode, startsAt: startsAt, stopsAt: now, sequenceNumber: 1);

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedBy, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_delegated_with_new_delegation_after_a_stopped_delegation()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithBalanceResponsiblePartyReceiverNumber(_delegatedBy.ActorNumber.Value)
            .Build();

        var startsAtForStoppedDelegation = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(10));
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode, startsAt: startsAtForStoppedDelegation, sequenceNumber: 0);

        // delegation, which stops previous delegation
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode, startsAt: startsAtForStoppedDelegation, stopsAt: startsAtForStoppedDelegation, sequenceNumber: 1);

        // new delegation, which starts delegation to _delegatedTo
        var startsAt = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5));
        var stopsAt = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5));
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode, startsAt: startsAt, stopsAt: stopsAt, sequenceNumber: 2);

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedTo, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_delegated_on_delegation_starts_date()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithBalanceResponsiblePartyReceiverNumber(_delegatedBy.ActorNumber.Value)
            .Build();

        var startsAt = Instant.FromUtc(2024, 10, 1, 0, 0);
        _clockStub.SetCurrentInstant(startsAt);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode, startsAt: startsAt, stopsAt: startsAt.Plus(Duration.FromDays(5)));

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedTo, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_on_delegation_stops_date()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithBalanceResponsiblePartyReceiverNumber(_delegatedBy.ActorNumber.Value)
            .Build();

        var stopsAt = Instant.FromUtc(2024, 10, 1, 0, 0);
        _clockStub.SetCurrentInstant(stopsAt);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode, startsAt: stopsAt.Minus(Duration.FromDays(5)), stopsAt: stopsAt);

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedBy, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_when_delegated_for_another_process_type()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithBalanceResponsiblePartyReceiverNumber(_delegatedBy.ActorNumber.Value)
            .Build();

        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.SeriesForBalanceResponsible.GridAreaCode, processType: ProcessType.ReceiveWholesaleResults);

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(_delegatedBy, _delegatedBy);
    }

    protected override void Dispose(bool disposing)
    {
        _context.Dispose();
        base.Dispose(disposing);
    }

    private static Actor CreateActor(ActorNumber actorNumber, ActorRole? actorRole = null)
    {
        return new Actor(actorNumber, actorRole ?? ActorRole.BalanceResponsibleParty);
    }

    private async Task AddMockDelegationsForActorAsync(Actor delegatedBy)
    {
        ArgumentNullException.ThrowIfNull(delegatedBy);
        await AddDelegationAsync(
            new(delegatedBy.ActorNumber, delegatedBy.ActorRole),
            new(ActorNumber.Create("8884567892341"), ActorRole.Delegated),
            "500",
            ProcessType.ReceiveWholesaleResults,
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)));
        await AddDelegationAsync(
            new(delegatedBy.ActorNumber, delegatedBy.ActorRole),
            new(ActorNumber.Create("8884567892342"), ActorRole.Delegated),
            "600",
            ProcessType.ReceiveWholesaleResults,
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(4)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(14)));
        await AddDelegationAsync(
            new(delegatedBy.ActorNumber, delegatedBy.ActorRole),
            new(ActorNumber.Create("8884567892343"), ActorRole.Delegated),
            "700",
            ProcessType.ReceiveWholesaleResults,
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(7)));
    }

    private async Task AddDelegationAsync(
        Actor delegatedBy,
        Actor delegatedTo,
        string gridAreaCode,
        ProcessType? processType = null,
        Instant? startsAt = null,
        Instant? stopsAt = null,
        int sequenceNumber = 0)
    {
        var masterDataClient = GetService<IMasterDataClient>();
        await masterDataClient.CreateProcessDelegationAsync(
            new ProcessDelegationDto(
                sequenceNumber,
                processType ?? ProcessType.ReceiveEnergyResults,
                gridAreaCode,
                startsAt ?? SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
                stopsAt ?? SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)),
                delegatedBy,
                delegatedTo),
            CancellationToken.None);
    }

    private async Task AssertEnqueuedOutgoingMessage(
        Actor receiverQueue,
        Actor receiverDocument,
        DocumentType? documentType = null,
        BusinessReason? businessReason = null)
    {
        ClearDbContextCaches();

        var outgoingMessage = await AssertOutgoingMessage.OutgoingMessageAsync(
            documentType != null ? documentType.Name : DocumentType.NotifyAggregatedMeasureData.Name,
            businessReason != null ? businessReason.Name : BusinessReason.BalanceFixing.Name,
            receiverQueue.ActorRole,
            GetService<IDatabaseConnectionFactory>(),
            GetService<IFileStorageClient>());
        outgoingMessage
            .HasReceiverId(receiverQueue.ActorNumber.Value)
            .HasReceiverRole(receiverQueue.ActorRole.Code)
            .HasDocumentReceiverId(receiverDocument.ActorNumber.Value)
            .HasDocumentReceiverRole(receiverDocument.ActorRole.Code);

        var result = await PeekMessageAsync(
            MessageCategory.Aggregations,
            actorNumber: receiverQueue.ActorNumber,
            actorRole: receiverQueue.ActorRole);

        AssertXmlMessage.Document(XDocument.Load(result!.Bundle))
            .IsDocumentType(documentType != null ? documentType : DocumentType.NotifyAggregatedMeasureData)
            .IsBusinessReason(businessReason != null ? businessReason : BusinessReason.BalanceFixing)
            .HasReceiverRole(receiverDocument.ActorRole)
            .HasReceiver(receiverQueue.ActorNumber)
            .HasSerieRecordCount(1);
    }

    private async Task<IEnumerable<Guid>> EnqueueAndCommitAsync(EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto message)
    {
        ClearDbContextCaches();
        return await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
    }
}
