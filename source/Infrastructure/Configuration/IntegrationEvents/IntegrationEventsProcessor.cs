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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration;
using Application.Configuration.DataAccess;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration.IntegrationEvents;

public class IntegrationEventsProcessor
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IMediator _mediator;
    private readonly ISystemDateTimeProvider _dateTimeProvider;
    private readonly List<IIntegrationEventMapper> _mappers;
    private readonly ILogger<IntegrationEventsProcessor> _logger;

    public IntegrationEventsProcessor(IDatabaseConnectionFactory connectionFactory, IMediator mediator, ISystemDateTimeProvider dateTimeProvider, IEnumerable<IIntegrationEventMapper> mappers, ILogger<IntegrationEventsProcessor> logger)
    {
        _connectionFactory = connectionFactory;
        _mediator = mediator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _mappers = mappers.ToList();
    }

    public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await FindPendingMessagesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var message in messages)
        {
            try
            {
                await _mediator.Publish(await MapperFor(message.EventType).MapFromAsync(message.EventPayload).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                await MarkAsProcessedAsync(message, cancellationToken).ConfigureAwait(false);
            }
            #pragma warning disable CA1031 // We dont' the type of exception here
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected Error when processing Integration Events. Id: {MessageId} and EventType: {MessageType}", message.Id, message.EventType);
                await MarkAsFailedAsync(message, e, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task MarkAsProcessedAsync(ReceivedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var updateStatement = $"UPDATE dbo.ReceivedIntegrationEvents " +
                              "SET ProcessedDate = @Now " +
                              "WHERE Id = @Id";
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(updateStatement, new
        {
            Id = message.Id,
            Now = _dateTimeProvider.Now().ToDateTimeUtc(),
        }).ConfigureAwait(false);
    }

    private async Task MarkAsFailedAsync(ReceivedIntegrationEvent message, Exception exception, CancellationToken cancellationToken)
    {
        var updateStatement = $"UPDATE dbo.ReceivedIntegrationEvents " +
                              "SET ProcessedDate = @Now, " +
                              "ErrorMessage = @ErrorMessage " +
                              "WHERE Id = @Id";
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(updateStatement, new
        {
            Id = message.Id,
            Now = _dateTimeProvider.Now().ToDateTimeUtc(),
            ErrorMessage = exception.ToString(),
        }).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ReceivedIntegrationEvent>> FindPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        var sql = "SELECT " +
                  $"Id AS {nameof(ReceivedIntegrationEvent.Id)}, " +
                  $"EventType AS {nameof(ReceivedIntegrationEvent.EventType)}, " +
                  $"EventPayload AS {nameof(ReceivedIntegrationEvent.EventPayload)} " +
                  "FROM dbo.ReceivedIntegrationEvents " +
                  "WHERE ProcessedDate IS NULL " +
                  "ORDER BY OccurredOn";

        var messages = await connection.QueryAsync<ReceivedIntegrationEvent>(sql).ConfigureAwait(false);
        return messages.ToList();
    }

    private IIntegrationEventMapper MapperFor(string eventType)
    {
        return _mappers.First(mapper => mapper.CanHandle(eventType));
    }
}
