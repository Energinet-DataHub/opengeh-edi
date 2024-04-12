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

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using FluentAssertions;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenEnqueueingOutgoingMessageWithDelegationTests : TestBase
{
    private readonly EnergyResultMessageDtoBuilder _energyResultMessageDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly ActorMessageQueueContext _context;
    private readonly SystemDateTimeProviderStub _dateTimeProvider;

    private ActorNumberAndRoleDto _delegatedBy = CreateActorNumberAndRole(ActorNumber.Create("1234567891234"));
    private ActorNumberAndRoleDto _delegatedTo = CreateActorNumberAndRole(ActorNumber.Create("1234567891235"), actorRole: ActorRole.Delegated);

    public WhenEnqueueingOutgoingMessageWithDelegationTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _energyResultMessageDtoBuilder = new EnergyResultMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        _context = GetService<ActorMessageQueueContext>();
        _dateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
    }

    [Fact]
    public async Task Enqueue_message_to_delegated()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedBy.ActorRole)
            .Build();

        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode);

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedTo, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_delegated_with_multiple_delegations()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedBy.ActorRole)
            .Build();

        await AddMockDelegationsForActorAsync(_delegatedBy);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode);

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedTo, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_delegated_as_grid_operator()
    {
        // Arrange
        _delegatedBy = CreateActorNumberAndRole(ActorNumber.Create("1234567891234"), actorRole: ActorRole.GridOperator);
        _delegatedTo = CreateActorNumberAndRole(ActorNumber.Create("1234567891235"), actorRole: ActorRole.GridOperator);
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedTo.ActorRole)
            .Build();

        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode);

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedTo, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_when_delegation_has_expired()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedBy.ActorRole)
            .Build();

        var endsAtInThePast = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(1));
        await AddDelegationAsync(
            _delegatedBy,
            _delegatedTo,
            message.Series.GridAreaCode,
            startsAt: SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(10)),
            stopsAt: endsAtInThePast);

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedBy, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_when_delegation_is_in_future()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedBy.ActorRole)
            .Build();

        var startsAtInTheFuture = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5));
        await AddDelegationAsync(
            _delegatedBy,
            _delegatedTo,
            message.Series.GridAreaCode,
            startsAt: startsAtInTheFuture,
            stopsAt: SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(10)));

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedBy, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_when_delegation_has_stopped()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedBy.ActorRole)
            .Build();

        var startsAt = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(10));
        var now = SystemClock.Instance.GetCurrentInstant();
        _dateTimeProvider.SetNow(now);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode, startsAt: startsAt, sequenceNumber: 0);

        // Newer delegation to original receiver, which stops previous delegation
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode, startsAt: startsAt, stopsAt: now, sequenceNumber: 1);

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedBy, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_delegated_with_new_delegation_after_a_stopped_delegation()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedBy.ActorRole)
            .Build();

        var startsAtForStoppedDelegation = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(10));
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode, startsAt: startsAtForStoppedDelegation, sequenceNumber: 0);

        // delegation, which stops previous delegation
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode, startsAt: startsAtForStoppedDelegation, stopsAt: startsAtForStoppedDelegation, sequenceNumber: 1);

        // new delegation, which starts delegation to _delegatedTo
        var startsAt = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5));
        var stopsAt = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5));
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode, startsAt: startsAt, stopsAt: stopsAt, sequenceNumber: 2);

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedTo, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_delegated_on_delegation_starts_date()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedBy.ActorRole)
            .Build();

        var startsAt = Instant.FromUtc(2024, 10, 1, 0, 0);
        _dateTimeProvider.SetNow(startsAt);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode, startsAt: startsAt, stopsAt: startsAt.Plus(Duration.FromDays(5)));

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedTo, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_on_delegation_stops_date()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedBy.ActorRole)
            .Build();

        var stopsAt = Instant.FromUtc(2024, 10, 1, 0, 0);
        _dateTimeProvider.SetNow(stopsAt);
        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode, startsAt: stopsAt.Minus(Duration.FromDays(5)), stopsAt: stopsAt);

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedBy, _delegatedBy);
    }

    [Fact]
    public async Task Enqueue_message_to_original_receiver_when_delegated_for_another_process_type()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(_delegatedBy.ActorNumber.Value)
            .WithReceiverRole(_delegatedBy.ActorRole)
            .Build();

        await AddDelegationAsync(_delegatedBy, _delegatedTo, message.Series.GridAreaCode, processType: ProcessType.ReceiveWholesaleResults);

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        await AssertEnqueuedOutgoingMessage(createdId, _delegatedBy, _delegatedBy);
    }

    protected override void Dispose(bool disposing)
    {
        _context.Dispose();
        base.Dispose(disposing);
    }

    private static ActorNumberAndRoleDto CreateActorNumberAndRole(ActorNumber actorNumber, ActorRole? actorRole = null)
    {
        return new ActorNumberAndRoleDto(actorNumber, actorRole ?? ActorRole.BalanceResponsibleParty);
    }

    private async Task AddMockDelegationsForActorAsync(ActorNumberAndRoleDto delegatedBy)
    {
        ArgumentNullException.ThrowIfNull(delegatedBy);
        await AddDelegationAsync(
            new ActorNumberAndRoleDto(delegatedBy.ActorNumber, delegatedBy.ActorRole),
            new ActorNumberAndRoleDto(ActorNumber.Create("8884567892341"), ActorRole.Delegated),
            "500",
            ProcessType.ReceiveWholesaleResults,
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)));
        await AddDelegationAsync(
            new ActorNumberAndRoleDto(delegatedBy.ActorNumber, delegatedBy.ActorRole),
            new ActorNumberAndRoleDto(ActorNumber.Create("8884567892342"), ActorRole.Delegated),
            "600",
            ProcessType.ReceiveWholesaleResults,
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(4)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(14)));
        await AddDelegationAsync(
            new ActorNumberAndRoleDto(delegatedBy.ActorNumber, delegatedBy.ActorRole),
            new ActorNumberAndRoleDto(ActorNumber.Create("8884567892343"), ActorRole.Delegated),
            "700",
            ProcessType.ReceiveWholesaleResults,
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(7)));
    }

    private async Task AddDelegationAsync(
        ActorNumberAndRoleDto delegatedBy,
        ActorNumberAndRoleDto delegatedTo,
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
        OutgoingMessageId createdId,
        ActorNumberAndRoleDto receiverQueue,
        ActorNumberAndRoleDto receiverDocument)
    {
        var enqueuedOutgoingMessage = await GetEnqueuedOutgoingMessageFromDatabase(createdId);
        enqueuedOutgoingMessage.ActorMessageQueueNumber.Should().Be(receiverQueue.ActorNumber.Value);
        enqueuedOutgoingMessage.ActorMessageQueueRole.Should().Be(receiverQueue.ActorRole.Code);
        enqueuedOutgoingMessage.DocumentReceiverNumber.Should().Be(receiverDocument.ActorNumber.Value);
        enqueuedOutgoingMessage.DocumentReceiverRole.Should().Be(receiverDocument.ActorRole.Code);

        var result = await PeekMessageAsync(
            MessageCategory.Aggregations,
            actorNumber: receiverQueue.ActorNumber,
            actorRole: receiverQueue.ActorRole);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.NotifyAggregatedMeasureData)
            .IsBusinessReason(BusinessReason.BalanceFixing)
            .HasReceiverRole(receiverDocument.ActorRole)
            .HasReceiver(receiverQueue.ActorNumber)
            .HasSerieRecordCount(1);
    }

    private async Task<(string ActorMessageQueueNumber, string ActorMessageQueueRole, string DocumentReceiverNumber, string DocumentReceiverRole)> GetEnqueuedOutgoingMessageFromDatabase(OutgoingMessageId createdId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var result = await connection.QuerySingleAsync(
            @"SELECT tOutgoing.ReceiverNumber, tOutgoing.ReceiverRole, tOutgoing.DocumentReceiverNumber, tOutgoing.DocumentReceiverRole
                    FROM [dbo].[OutgoingMessages] AS tOutgoing
                    WHERE tOutgoing.Id = @Id",
            new
                {
                    Id = createdId.Value.ToString(),
                });

        return (
            ActorMessageQueueNumber: result.ReceiverNumber,
            ActorMessageQueueRole: result.ReceiverRole,
            DocumentReceiverNumber: result.DocumentReceiverNumber,
            DocumentReceiverRole: result.DocumentReceiverRole);
    }

    private async Task<OutgoingMessageId> EnqueueAndCommitAsync(EnergyResultMessageDto message)
    {
        return await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
    }
}
