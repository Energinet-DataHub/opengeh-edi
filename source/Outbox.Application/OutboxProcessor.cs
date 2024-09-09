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

using Energinet.DataHub.EDI.Outbox.Domain;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using Energinet.DataHub.EDI.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.Outbox.Application;

public class OutboxProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOutboxRepository outboxRepository,
    IClock clock,
    ILogger<OutboxProcessor> logger)
    : IOutboxProcessor
{
    private readonly IOutboxRepository _outboxRepository = outboxRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IClock _clock = clock;
    private readonly ILogger<OutboxProcessor> _logger = logger;

    public async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        var outboxMessageIds = await _outboxRepository.GetUnprocessedOutboxMessageIdsAsync(cancellationToken)
            .ConfigureAwait(false);

        if (outboxMessageIds.Count > 0)
            _logger.LogInformation("Processing {OutboxMessageCount} outbox messages", outboxMessageIds.Count);

        foreach (var outboxMessageId in outboxMessageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ProcessOutboxMessageAsync(outboxMessageId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SetAsFailedAsync(outboxMessageId, ex)
                    .ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Process outbox message in a new scope, to avoid situations where one message failing stops future messages
    /// from processing.
    /// <remarks>
    /// Uses CancellationToken until the outbox message has begun publishing, after processing
    /// have begun we want to save the changes before cancelling the task.
    /// </remarks>
    /// </summary>
    private async Task ProcessOutboxMessageAsync(OutboxMessageId outboxMessageId, CancellationToken cancellationToken)
    {
        using var innerScope = _serviceScopeFactory.CreateScope();
        var outboxContext = innerScope.ServiceProvider.GetRequiredService<OutboxContext>();
        var repository = innerScope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var outboxMessagePublishers = innerScope.ServiceProvider.GetServices<IOutboxPublisher>();

        var outboxMessage = await repository.GetAsync(outboxMessageId, cancellationToken)
            .ConfigureAwait(false);

        if (!outboxMessage.ShouldProcessNow(_clock))
            return;

        try
        {
            outboxMessage.SetAsProcessing(_clock);
            await outboxContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Outbox message with id {OutboxMessageId} processing was already started",
                outboxMessageId);
            return;
        }

        // Process outbox message
        var outboxMessagePublisher = outboxMessagePublishers
            .SingleOrDefault(p => p.CanPublish(outboxMessage.Type));

        if (outboxMessagePublisher == null)
            throw new InvalidOperationException($"No processor found for outbox message type {outboxMessage.Type} and id {outboxMessage.Id}");

        await outboxMessagePublisher.PublishAsync(outboxMessage.Payload)
            .ConfigureAwait(false);

        outboxMessage.SetAsProcessed(_clock);

        await outboxContext
            // ReSharper disable once MethodSupportsCancellation
            // We want to save the changes before cancelling the task, since the outbox message is already published
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Set as failed in a new scope, to avoid situations where the exception is cached on the existing db context.
    /// <remarks>Uses CancellationToken.None since we want to save the error even if cancellation is requested</remarks>
    /// </summary>
    private async Task SetAsFailedAsync(OutboxMessageId outboxMessageId, Exception exception)
    {
        _logger.LogError(
            exception,
            "Failed to process outbox message with id {OutboxMessageId}",
            outboxMessageId);

        using var errorScope = _serviceScopeFactory.CreateScope();
        var outboxContext = errorScope.ServiceProvider.GetRequiredService<OutboxContext>();
        var repository = errorScope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var outgoingMessage = await repository.GetAsync(outboxMessageId, CancellationToken.None)
            .ConfigureAwait(false);

        outgoingMessage.SetAsFailed(_clock, exception.ToString());

        await outboxContext.SaveChangesAsync(CancellationToken.None)
            .ConfigureAwait(false);
    }
}
