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
using System.Linq;
using System.Threading.Tasks;
using Application.Configuration;
using Application.Configuration.DataAccess;
using Dapper;
using Infrastructure.Configuration.IntegrationEvents;
using MediatR;

namespace Infrastructure.Configuration.Processing.Inbox;

public class InboxProcessor
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IMediator _mediator;
    private readonly ISystemDateTimeProvider _dateTimeProvider;
    private readonly List<IIntegrationEventMapper> _mappers;

    public InboxProcessor(IDatabaseConnectionFactory connectionFactory, IMediator mediator, ISystemDateTimeProvider dateTimeProvider, IEnumerable<IIntegrationEventMapper> mappers)
    {
        _connectionFactory = connectionFactory;
        _mediator = mediator;
        _dateTimeProvider = dateTimeProvider;
        _mappers = mappers.ToList();
    }

    public async Task ProcessMessagesAsync()
    {
        var messages = await FindPendingMessagesAsync().ConfigureAwait(false);

        foreach (var message in messages)
        {
            try
            {
                await _mediator.Publish(MapperFor(message.EventType).MapFrom(message.EventPayload)).ConfigureAwait(false);
                await MarkAsProcessedAsync(message).ConfigureAwait(false);
            }
            #pragma warning disable CA1031 // We dont' the type of exception here
            catch (Exception e)
            {
                await MarkAsFailedAsync(message, e).ConfigureAwait(false);
            }
        }
    }

    private async Task MarkAsProcessedAsync(InboxMessage message)
    {
        var updateStatement = $"UPDATE b2b.InboxMessages " +
                              "SET ProcessedDate = @Now " +
                              "WHERE Id = @Id";
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        await connection.ExecuteAsync(updateStatement, new
        {
            Id = message.Id,
            Now = _dateTimeProvider.Now().ToDateTimeUtc(),
        }).ConfigureAwait(false);
    }

    private async Task MarkAsFailedAsync(InboxMessage message, Exception exception)
    {
        var updateStatement = $"UPDATE b2b.InboxMessages " +
                              "SET ProcessedDate = @Now, " +
                              "ErrorMessage = @ErrorMessage " +
                              "WHERE Id = @Id";
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        await connection.ExecuteAsync(updateStatement, new
        {
            Id = message.Id,
            Now = _dateTimeProvider.Now().ToDateTimeUtc(),
            ErrorMessage = exception.ToString(),
        }).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<InboxMessage>> FindPendingMessagesAsync()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        var sql = "SELECT " +
                  $"Id AS {nameof(InboxMessage.Id)}, " +
                  $"EventType AS {nameof(InboxMessage.EventType)}, " +
                  $"EventPayload AS {nameof(InboxMessage.EventPayload)} " +
                  "FROM b2b.InboxMessages " +
                  "WHERE ProcessedDate IS NULL " +
                  "ORDER BY OccurredOn";

        var messages = await connection.QueryAsync<InboxMessage>(sql).ConfigureAwait(false);
        return messages.ToList();
    }

    private IIntegrationEventMapper MapperFor(string eventType)
    {
        return _mappers.First(mapper => mapper.CanHandle(eventType));
    }
}
