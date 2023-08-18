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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Application.OutgoingMessages.Dequeue;
using Application.OutgoingMessages.Peek;
using Dapper;
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using Infrastructure.OutgoingMessages.Queueing;
using IntegrationTests.Fixtures;
using NodaTime.Extensions;
using Xunit;
using Point = Domain.Transactions.Aggregations.Point;

namespace IntegrationTests.Application.OutgoingMessages;

public class WhenEnqueueingTests : TestBase
{
    private readonly MessageEnqueuer _messageEnqueuer;

    public WhenEnqueueingTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _messageEnqueuer = GetService<MessageEnqueuer>();
    }

    [Fact]
    public async Task Outgoing_message_is_enqueued()
    {
        var message = CreateOutgoingMessage();
        await EnqueueMessage(message);

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await
            connection
                .QuerySingleOrDefaultAsync(sql)
                .ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(result.DocumentType, message.DocumentType.Name);
        Assert.Equal(result.ReceiverId, message.ReceiverId.Value);
        Assert.Equal(result.ReceiverRole, message.ReceiverRole.Name);
        Assert.Equal(result.SenderId, message.SenderId.Value);
        Assert.Equal(result.SenderRole, message.SenderRole.Name);
        Assert.Equal(result.BusinessReason, message.BusinessReason);
        Assert.NotNull(result.MessageRecord);
        Assert.NotNull(result.AssignedBundleId);
    }

    [Fact]
    public async Task Ensure_outgoing_messages_is_enqueued_in_the_same_actor_queue()
    {
        var message = CreateOutgoingMessage();
        await _messageEnqueuer.EnqueueAsync(message);
        await _messageEnqueuer.EnqueueAsync(message);

        var unitOfWork = GetService<IUnitOfWork>();
        await unitOfWork.CommitAsync();

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var sql = "SELECT * FROM [dbo].[ActorMessageQueues]";
        var result = (await
            connection
                .QueryAsync(sql)
                .ConfigureAwait(false)).ToList();

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task Can_peek_message()
    {
        var message = CreateOutgoingMessage();
        await EnqueueMessage(message);
        var command = new PeekCommand(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, DocumentFormat.Xml);

        var result = await InvokeCommandAsync(command);

        Assert.NotNull(result.MessageId);
    }

    [Fact]
    public async Task Can_dequeue_bundle()
    {
        var message = CreateOutgoingMessage();
        await EnqueueMessage(message);
        var peekCommand = new PeekCommand(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, DocumentFormat.Xml);
        var peekResult = await InvokeCommandAsync(peekCommand);
        var dequeueCommand = new DequeueCommand(peekResult.MessageId!.Value.ToString(), message.ReceiverRole, message.ReceiverId);

        var result = await InvokeCommandAsync(dequeueCommand);

        Assert.True(result.Success);
    }

    private static OutgoingMessage CreateOutgoingMessage()
    {
        var p = new Point(1, 1m, Quality.Calculated.Name, "2022-12-12T23:00:00Z"); //TODO tilføj point
        var points = Array.Empty<Point>();
        var message = AggregationResultMessage.Create(
            ActorNumber.Create("1234567891912"),
            MarketRole.MeteringDataAdministrator,
            TransactionId.Create(Guid.NewGuid()),
            new Aggregation(
                points,
                MeteringPointType.Consumption.Name,
                MeasurementUnit.Kwh.Name,
                Resolution.Hourly.Name,
                new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
                SettlementType.NonProfiled.Name,
                BusinessReason.BalanceFixing.Name,
                new ActorGrouping("1234567891911", null),
                new GridAreaDetails("805", "1234567891045")));
        return message;
    }

    private async Task EnqueueMessage(OutgoingMessage message)
    {
        await _messageEnqueuer.EnqueueAsync(message);
        var unitOfWork = GetService<IUnitOfWork>();
        await unitOfWork.CommitAsync();
    }
}
