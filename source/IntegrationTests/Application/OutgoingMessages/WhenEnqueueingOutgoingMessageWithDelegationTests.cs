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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenEnqueueingOutgoingMessageWithDelegationTests : WhenEnqueueingOutgoingMessageTests
{
    private readonly EnergyResultMessageDtoBuilder _energyResultMessageDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly ActorMessageQueueContext _context;

    public WhenEnqueueingOutgoingMessageWithDelegationTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _energyResultMessageDtoBuilder = new EnergyResultMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        _context = GetService<ActorMessageQueueContext>();
    }

    [Fact]
    public async Task Can_peek_message_as_delegated_to()
    {
        // Arrange
        var delegatedBy = new { ActorNumber = ActorNumber.Create("123"), ActorRole = ActorRole.BalanceResponsibleParty };
        var delegatedTo = new { ActorNumber = ActorNumber.Create("456"), ActorRole = ActorRole.Delegated };
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(delegatedBy.ActorNumber.Value)
            .WithReceiverRole(delegatedTo.ActorRole)
            .Build();

        AddDelegation(
            delegatedBy.ActorNumber,
            delegatedBy.ActorRole,
            delegatedTo.ActorNumber,
            delegatedTo.ActorRole,
            message.Series.GridAreaCode,
            DocumentType.NotifyAggregatedMeasureData,
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)));

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var enqueuedOutgoingMessage = await GetEnqueuedOutgoingMessageFromDatabase(createdId);

        enqueuedOutgoingMessage.ActorMessageQueueNumber.Should().Be(delegatedTo.ActorNumber.Value);
        enqueuedOutgoingMessage.ActorMessageQueueRole.Should().Be(delegatedTo.ActorRole.Code);
        enqueuedOutgoingMessage.OutgoingMessageReceiverNumber.Should().Be(delegatedBy.ActorNumber.Value);
        enqueuedOutgoingMessage.OutgoingMessageReceiverRole.Should().Be(delegatedBy.ActorRole.Code);
    }

    [Fact]
    public async Task Can_peek_message_as_delegated_to_and_grid_operator()
    {
        // Arrange
        var delegatedBy = new { ActorNumber = ActorNumber.Create("123"), ActorRole = ActorRole.GridOperator };
        var delegatedTo = new { ActorNumber = ActorNumber.Create("456"), ActorRole = ActorRole.GridOperator };
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(delegatedBy.ActorNumber.Value)
            .WithReceiverRole(delegatedTo.ActorRole)
            .Build();

        AddDelegation(
            delegatedBy.ActorNumber,
            delegatedBy.ActorRole,
            delegatedTo.ActorNumber,
            delegatedTo.ActorRole,
            message.Series.GridAreaCode,
            DocumentType.NotifyAggregatedMeasureData,
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)));

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var enqueuedOutgoingMessage = await GetEnqueuedOutgoingMessageFromDatabase(createdId);

        enqueuedOutgoingMessage.ActorMessageQueueNumber.Should().Be(delegatedTo.ActorNumber.Value);
        enqueuedOutgoingMessage.ActorMessageQueueRole.Should().Be(delegatedTo.ActorRole.Code);
        enqueuedOutgoingMessage.OutgoingMessageReceiverNumber.Should().Be(delegatedBy.ActorNumber.Value);
        enqueuedOutgoingMessage.OutgoingMessageReceiverRole.Should().Be(delegatedBy.ActorRole.Code);
    }

    [Fact]
    public async Task Can_not_peek_message_as_delegated_by()
    {
        // Arrange
        var delegatedBy = new { ActorNumber = ActorNumber.Create("123"), ActorRole = ActorRole.BalanceResponsibleParty };
        var delegatedTo = new { ActorNumber = ActorNumber.Create("456"), ActorRole = ActorRole.Delegated };
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(delegatedBy.ActorNumber.Value)
            .WithReceiverRole(delegatedTo.ActorRole)
            .Build();

        AddDelegation(
            delegatedBy.ActorNumber,
            delegatedBy.ActorRole,
            delegatedTo.ActorNumber,
            delegatedTo.ActorRole,
            message.Series.GridAreaCode,
            DocumentType.NotifyAggregatedMeasureData,
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)));

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var enqueuedOutgoingMessage = await GetEnqueuedOutgoingMessageFromDatabase(createdId);

        enqueuedOutgoingMessage.ActorMessageQueueNumber.Should().NotBe(delegatedBy.ActorNumber.Value);
        enqueuedOutgoingMessage.ActorMessageQueueRole.Should().NotBe(delegatedBy.ActorRole.Code);
        enqueuedOutgoingMessage.OutgoingMessageReceiverNumber.Should().Be(delegatedBy.ActorNumber.Value);
        enqueuedOutgoingMessage.OutgoingMessageReceiverRole.Should().Be(delegatedBy.ActorRole.Code);
    }

    [Fact]
    public async Task Can_peek_message_as_delegated_by_when_delegation_has_expired()
    {
        // Arrange
        var delegatedBy = new { ActorNumber = ActorNumber.Create("123"), ActorRole = ActorRole.MeteredDataAdministrator };
        var delegatedTo = new { ActorNumber = ActorNumber.Create("465"), ActorRole = ActorRole.Delegated };
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(delegatedBy.ActorNumber.Value)
            .WithReceiverRole(delegatedTo.ActorRole)
            .Build();

        AddDelegation(
            delegatedBy.ActorNumber,
            delegatedBy.ActorRole,
            delegatedTo.ActorNumber,
            delegatedTo.ActorRole,
            message.Series.GridAreaCode,
            DocumentType.NotifyAggregatedMeasureData,
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(10)),
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)));

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var enqueuedOutgoingMessage = await GetEnqueuedOutgoingMessageFromDatabase(createdId);

        enqueuedOutgoingMessage.ActorMessageQueueNumber.Should().Be(delegatedBy.ActorNumber.Value);
        enqueuedOutgoingMessage.ActorMessageQueueRole.Should().Be(delegatedBy.ActorRole.Code);
        enqueuedOutgoingMessage.OutgoingMessageReceiverNumber.Should().Be(delegatedBy.ActorNumber.Value);
        enqueuedOutgoingMessage.OutgoingMessageReceiverRole.Should().Be(delegatedBy.ActorRole.Code);
    }

    [Fact]
    public async Task Can_peek_message_as_delegated_by_when_delegation_is_in_future()
    {
        // Arrange
        var delegatedBy = new { ActorNumber = ActorNumber.Create("123"), ActorRole = ActorRole.MeteredDataAdministrator };
        var delegatedTo = new { ActorNumber = ActorNumber.Create("456"), ActorRole = ActorRole.Delegated };
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(delegatedBy.ActorNumber.Value)
            .WithReceiverRole(delegatedTo.ActorRole)
            .Build();

        AddDelegation(
            delegatedBy.ActorNumber,
            delegatedBy.ActorRole,
            delegatedTo.ActorNumber,
            delegatedTo.ActorRole,
            message.Series.GridAreaCode,
            DocumentType.NotifyAggregatedMeasureData,
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)),
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(10)));

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var enqueuedOutgoingMessage = await GetEnqueuedOutgoingMessageFromDatabase(createdId);

        enqueuedOutgoingMessage.ActorMessageQueueNumber.Should().Be(delegatedBy.ActorNumber.Value);
        enqueuedOutgoingMessage.ActorMessageQueueRole.Should().Be(delegatedBy.ActorRole.Code);
        enqueuedOutgoingMessage.OutgoingMessageReceiverNumber.Should().Be(delegatedBy.ActorNumber.Value);
        enqueuedOutgoingMessage.OutgoingMessageReceiverRole.Should().Be(delegatedBy.ActorRole.Code);
    }

    protected override void Dispose(bool disposing)
    {
        _context.Dispose();
        base.Dispose(disposing);
    }

    private async Task<(string ActorMessageQueueNumber, string ActorMessageQueueRole, string OutgoingMessageReceiverNumber, string OutgoingMessageReceiverRole)> GetEnqueuedOutgoingMessageFromDatabase(OutgoingMessageId createdId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var result = await connection.QuerySingleAsync(
            @"SELECT tQueue.ActorNumber, tQueue.ActorRole, tOutgoing.ReceiverNumber, tOutgoing.ReceiverRole
                    FROM [dbo].[OutgoingMessages] AS tOutgoing WHERE tOutgoing.Id = @Id
                        INNER JOIN [dbo].[Bundles] as tBundle ON tOutgoing.AssignedBundleId = tBundle.Id
                        INNER JOIN [dbo].ActorMessageQueues as tQueue on tBundle.ActorMessageQueueId = tQueue.Id",
            new
                {
                    Id = createdId.Value.ToString(),
                });

        return (
            ActorMessageQueueNumber: result.ActorNumber,
            ActorMessageQueueRole: result.ActorRole,
            OutgoingMessageReceiverNumber: result.ReceiverNumber,
            OutgoingMessageReceiverRole: result.ReceiverRole);
    }

    private async Task<OutgoingMessageId> EnqueueAndCommitAsync(EnergyResultMessageDto message)
    {
        return await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
    }
}
