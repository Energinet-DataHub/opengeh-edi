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
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenEnqueueingTests : TestBase
{
    private readonly OutgoingMessageDtoBuilder _outgoingMessageDtoBuilder;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IOutGoingMessagesClient _outgoingMessagesClient;
    private readonly ActorMessageQueueContext _context;

    public WhenEnqueueingTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _outgoingMessageDtoBuilder = new OutgoingMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutGoingMessagesClient>();
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
        _context = GetService<ActorMessageQueueContext>();
    }

    [Fact]
    public async Task Outgoing_message_is_enqueued()
    {
        var message = _outgoingMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await
            connection
                .QuerySingleOrDefaultAsync(sql);
        Assert.NotNull(result);
        Assert.Equal(result.DocumentType, DocumentType.NotifyAggregatedMeasureData.Name);
        Assert.Equal(result.ReceiverId, message.ReceiverId.Value);
        Assert.Equal(result.ReceiverRole, message.ReceiverRole.Name);
        Assert.Equal(result.SenderId, message.SenderId.Value);
        Assert.Equal(result.SenderRole, message.SenderRole.Name);
        Assert.Equal(result.BusinessReason, message.BusinessReason);
        Assert.NotNull(result.MessageRecord);
        Assert.NotNull(result.AssignedBundleId);
    }

    [Fact]
    public async Task Can_peek_message()
    {
        var message = _outgoingMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);

        var result = await _outgoingMessagesClient.PeekAndCommitAsync(
            new PeekRequestDto(
                message.ReceiverId,
                MessageCategory.Aggregations,
                message.ReceiverRole,
                DocumentFormat.Xml),
            CancellationToken.None);

        Assert.NotNull(result.MessageId);
    }

    [Fact]
    public async Task Can_peek_oldest_bundle()
    {
        var message = _outgoingMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);
        ((SystemDateTimeProviderStub)_systemDateTimeProvider).SetNow(_systemDateTimeProvider.Now().PlusSeconds(1));
        var message2 = _outgoingMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message2);

        var result = await _outgoingMessagesClient.PeekAndCommitAsync(new PeekRequestDto(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, DocumentFormat.Ebix), CancellationToken.None);
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT top 1 id FROM [dbo].[Bundles] order by created";
        var id = await
            connection
                .QuerySingleOrDefaultAsync<Guid>(sql);

        Assert.Equal(result.MessageId, id);
    }

    [Fact]
    public async Task Can_dequeue_bundle()
    {
        var message = _outgoingMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);
        var peekCommand = new PeekRequestDto(
            message.ReceiverId,
            MessageCategory.Aggregations,
            message.ReceiverRole,
            DocumentFormat.Xml);
        var peekResult = await _outgoingMessagesClient.PeekAndCommitAsync(peekCommand, CancellationToken.None);
        var dequeueCommand = new DequeueRequestDto(
            peekResult.MessageId!.Value.ToString(),
            message.ReceiverRole,
            message.ReceiverId);

        var result = await _outgoingMessagesClient.DequeueAndCommitAsync(dequeueCommand, CancellationToken.None);

        Assert.True(result.Success);
    }

    protected override void Dispose(bool disposing)
    {
        _context.Dispose();
        base.Dispose(disposing);
    }

    private async Task EnqueueAndCommitAsync(OutgoingMessageDto message)
    {
        await _outgoingMessagesClient.EnqueueAsync(message);
        await _context.SaveChangesAsync();
    }
}
