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
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages;
using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;
using NodaTime.Extensions;
using Xunit;
using IUnitOfWork = Energinet.DataHub.EDI.Domain.IUnitOfWork;
using Point = Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.Point;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenEnqueueingTests : ProcessTestBase
{
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;

    public WhenEnqueueingTests(ProcessDatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _outgoingMessageRepository = GetService<IOutgoingMessageRepository>();
    }

    [Fact]
    public async Task Outgoing_message_is_enqueued()
    {
        var command = CreateEnqueueCommand();
        await InvokeCommandAsync(command);

        // TODO: (LRN) Ensure we have a ActorQueue with a bundle with the expected OutgoingMessage.
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await
            connection
                .QuerySingleOrDefaultAsync(sql);
        Assert.NotNull(result);
        Assert.Equal(result.DocumentType, MessageCategory.Aggregations);
        Assert.Equal(result.ReceiverId, command.OutgoingMessageDto.ReceiverId.Value);
        Assert.Equal(result.ReceiverRole, command.OutgoingMessageDto.ReceiverRole.Name);
        Assert.Equal(result.SenderId, command.OutgoingMessageDto.SenderId.Value);
        Assert.Equal(result.SenderRole, command.OutgoingMessageDto.SenderRole.Name);
        Assert.Equal(result.BusinessReason, command.OutgoingMessageDto.BusinessReason);
        Assert.NotNull(result.MessageRecord);
        Assert.NotNull(result.AssignedBundleId);
    }

    [Fact]
    public async Task Can_peek_message()
    {
        var enqueueCommand = CreateEnqueueCommand();
        await InvokeCommandAsync(enqueueCommand);
        var command = new PeekCommand(
            enqueueCommand.OutgoingMessageDto.ReceiverId,
            MessageCategory.Aggregations,
            enqueueCommand.OutgoingMessageDto.ReceiverRole,
            DocumentFormat.Xml);

        var result = await InvokeCommandAsync(command);

        Assert.NotNull(result.MessageId);
    }

    [Fact]
    public async Task Can_dequeue_bundle()
    {
        var enqueueCommand = CreateEnqueueCommand();
        await InvokeCommandAsync(enqueueCommand);
        var peekCommand = new PeekCommand(
            enqueueCommand.OutgoingMessageDto.ReceiverId,
            MessageCategory.Aggregations,
            enqueueCommand.OutgoingMessageDto.ReceiverRole,
            DocumentFormat.Xml);
        var peekResult = await InvokeCommandAsync(peekCommand);
        var dequeueCommand = new DequeueCommand(
            peekResult.MessageId!.Value.ToString(),
            enqueueCommand.OutgoingMessageDto.ReceiverRole,
            enqueueCommand.OutgoingMessageDto.ReceiverId);

        var result = await InvokeCommandAsync(dequeueCommand);

        Assert.True(result.Success);
    }

    private static EnqueueMessageCommand CreateEnqueueCommand()
    {
        var p = new Point(1, 1m, Quality.Calculated.Name, "2022-12-12T23:00:00Z"); //TODO tilføj point
        IReadOnlyList<Process.Domain.Transactions.Aggregations.OutgoingMessage.Point> points = new List<Process.Domain.Transactions.Aggregations.OutgoingMessage.Point>();
        var message = AggregationResultMessage.Create(
            ActorNumber.Create("1234567891912"),
            MarketRole.MeteringDataAdministrator,
            ProcessId.Create(Guid.NewGuid()).Id,
            new GridAreaDetails("805", "1234567891045").GridAreaCode,
            MeteringPointType.Consumption.Name,
            SettlementType.NonProfiled.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.Hourly.Name,
            null,
            "1234567891911",
            new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
            points,
            BusinessReason.BalanceFixing.Name);
        return new EnqueueMessageCommand(message);
    }
}
