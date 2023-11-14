﻿// Copyright 2020 Energinet DataHub A/S
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
using Dapper;
using Energinet.DataHub.EDI.Common.DataAccess;
using Energinet.DataHub.EDI.Common.DateTime;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Infrastructure.InboxEvents;

public class InboxEventsProcessor
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IMediator _mediator;
    private readonly ISystemDateTimeProvider _dateTimeProvider;
    private readonly ILogger<InboxEventsProcessor> _logger;
    private readonly List<IInboxEventMapper> _mappers;

    public InboxEventsProcessor(
        IDatabaseConnectionFactory connectionFactory,
        IMediator mediator,
        ISystemDateTimeProvider dateTimeProvider,
        IEnumerable<IInboxEventMapper> mappers,
        ILogger<InboxEventsProcessor> logger)
    {
        _connectionFactory = connectionFactory;
        _mediator = mediator;
        _dateTimeProvider = dateTimeProvider;
        _mappers = mappers.ToList();
        _logger = logger;
    }

    public async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        var inboxEvents = await FindPendingMessagesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var inboxEvent in inboxEvents)
        {
            try
            {
                var notification = await MapperFor(inboxEvent.EventType)
                    .MapFromAsync(inboxEvent.EventPayload, inboxEvent.ReferenceId, cancellationToken).ConfigureAwait(false);

                await _mediator.Publish(
                        notification,
                        cancellationToken)
                    .ConfigureAwait(false);

                await MarkAsProcessedAsync(inboxEvent, cancellationToken).ConfigureAwait(false);
            }
            #pragma warning disable CA1031 // Multiple exceptions can be causing a process failed.
            catch (Exception e)
            {
                await MarkAsFailedAsync(inboxEvent, e, cancellationToken).ConfigureAwait(false);
                _logger.LogError(e, "Failed to process inbox event. Id: {id}, EventType: {@Event}", inboxEvent.Id, inboxEvent.EventType);
            }
        }
    }

    private async Task MarkAsProcessedAsync(ReceivedInboxEvent @event, CancellationToken cancellationToken)
    {
        var updateStatement = $"UPDATE dbo.ReceivedInboxEvents " +
                              "SET ProcessedDate = @Now " +
                              "WHERE Id = @Id";
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        _ = await connection.ExecuteAsync(updateStatement, new
        {
            Id = @event.Id,
            Now = _dateTimeProvider.Now().ToDateTimeUtc(),
        }).ConfigureAwait(false);
    }

    private async Task MarkAsFailedAsync(ReceivedInboxEvent @event, Exception exception, CancellationToken cancellationToken)
    {
        var updateStatement = $"UPDATE dbo.ReceivedInboxEvents " +
                              "SET ProcessedDate = @Now, " +
                              "ErrorMessage = @ErrorMessage " +
                              "WHERE Id = @Id";
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(updateStatement, new
        {
            Id = @event.Id,
            Now = _dateTimeProvider.Now().ToDateTimeUtc(),
            ErrorMessage = exception.ToString(),
        }).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ReceivedInboxEvent>> FindPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        var sql = "SELECT " +
                  $"Id AS {nameof(ReceivedInboxEvent.Id)}, " +
                  $"EventType AS {nameof(ReceivedInboxEvent.EventType)}, " +
                  $"EventPayload AS {nameof(ReceivedInboxEvent.EventPayload)}, " +
                  $"ReferenceId as {nameof(ReceivedInboxEvent.ReferenceId)} " +
                  "FROM dbo.ReceivedInboxEvents " +
                  "WHERE ProcessedDate IS NULL " +
                  "ORDER BY OccurredOn";

        var messages = await connection.QueryAsync<ReceivedInboxEvent>(sql).ConfigureAwait(false);
        return messages.ToList();
    }

    private IInboxEventMapper MapperFor(string eventType)
    {
        var mapper = _mappers.FirstOrDefault(mapper => mapper.CanHandle(eventType))
                     ?? throw new UnsupportedInboxEventTypeException($"No InboxEventMapper for {eventType} eventType was found.");
        return mapper;
    }
}
