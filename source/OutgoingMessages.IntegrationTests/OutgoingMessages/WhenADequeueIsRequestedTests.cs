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

using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Dequeue;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.Tests.Factories;
using FluentAssertions;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages;

public class WhenADequeueIsRequestedTests : OutgoingMessagesTestBase
{
    private readonly EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly AcceptedEnergyResultMessageDtoBuilder _energyResultPerGridAreaMessageDtoBuilder;

    public WhenADequeueIsRequestedTests(
        OutgoingMessagesTestFixture outgoingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
        _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder =
            new EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder();
        _energyResultPerGridAreaMessageDtoBuilder = new AcceptedEnergyResultMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
    }

    [Fact]
    public async Task Dequeue_is_unsuccessful_when_bundle_does_not_exist()
    {
        var dequeueResult = await _outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(Guid.NewGuid().ToString(), ActorRole.EnergySupplier, ActorNumber.Create(SampleData.SenderId)), CancellationToken.None);

        Assert.False(dequeueResult.Success);
    }

    [Fact]
    public async Task Dequeue_unknown_message_id_is_unsuccessful_when_actor_has_a_queue()
    {
        var unknownMessageId = Guid.NewGuid().ToString();
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);

        ClearDbContextCaches();
        var dequeueResult = await _outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(unknownMessageId, ActorRole.EnergySupplier, ActorNumber.Create(SampleData.SenderId)), CancellationToken.None);

        Assert.False(dequeueResult.Success);
    }

    [Fact]
    public async Task Dequeue_is_Successful()
    {
        var message = _energyResultPerGridAreaMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await _outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        var context = GetService<ActorMessageQueueContext>();
        await context.SaveChangesAsync();

        ClearDbContextCaches();
        var peekResult = await _outgoingMessagesClient.PeekAndCommitAsync(
            new PeekRequestDto(
            ActorNumber.Create(SampleData.NewEnergySupplierNumber),
            MessageCategory.Aggregations,
            ActorRole.EnergySupplier,
            DocumentFormat.Xml),
            CancellationToken.None);

        ClearDbContextCaches();
        var dequeueResult = await _outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(peekResult!.MessageId.Value, ActorRole.EnergySupplier, ActorNumber.Create(SampleData.SenderId)), CancellationToken.None);
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var found = await connection
            .QuerySingleOrDefaultAsync<Instant?>("SELECT DequeuedAt FROM [dbo].Bundles");

        Assert.True(dequeueResult.Success);
        Assert.NotNull(found);
    }

    /// <summary>
    /// This test verifies the "hack" for a MDR/GridOperator actor which is the same Actor but with two distinct roles MDR and GridOperator
    /// The actor uses the MDR (MeteredDataResponsible) role when making request (RequestAggregatedMeasureData)
    /// but uses the DDM (GridOperator) role when peeking.
    /// This means that when dequeuing as a MDR we should dequeue the DDM queue
    /// </summary>
    [Fact]
    public async Task When_DequeuingAsMeteredDataResponsible_Then_DequeuesGridOperatorMessages()
    {
        // Arrange
        var actorNumber = ActorNumber.Create(SampleData.SenderId);
        var message = _energyResultPerGridAreaMessageDtoBuilder
            .WithReceiverNumber(actorNumber.Value)
            .WithReceiverRole(ActorRole.GridAccessProvider)
            .Build();

        await _outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        var context = GetService<ActorMessageQueueContext>();
        await context.SaveChangesAsync();

        ClearDbContextCaches();
        var peekResult = await _outgoingMessagesClient.PeekAndCommitAsync(
            new PeekRequestDto(
                actorNumber,
                MessageCategory.Aggregations,
                ActorRole.MeteredDataResponsible,
                DocumentFormat.Xml),
            CancellationToken.None);

        // Act
        ClearDbContextCaches();
        var dequeueResult = await _outgoingMessagesClient.DequeueAndCommitAsync(
            new DequeueRequestDto(
                peekResult!.MessageId.Value,
                ActorRole.MeteredDataResponsible,
                actorNumber),
            CancellationToken.None);

        // Assert
        dequeueResult.Success.Should().BeTrue();
    }
}
