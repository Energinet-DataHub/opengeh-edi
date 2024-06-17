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
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;
using Microsoft.Extensions.Logging;
using IIntegrationEventHandler = Energinet.DataHub.Core.Messaging.Communication.Subscriber.IIntegrationEventHandler;

namespace Energinet.DataHub.EDI.IntegrationEvents.Application;

#pragma warning disable CA1711
public sealed class IntegrationEventHandler : IIntegrationEventHandler
#pragma warning restore CA1711
{
    private readonly ILogger<IntegrationEventHandler> _logger;
    private readonly IReceivedIntegrationEventRepository _receivedIntegrationEventRepository;
    private readonly IReadOnlyDictionary<string, IIntegrationEventProcessor> _integrationEventProcessors;
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

    public IntegrationEventHandler(
        ILogger<IntegrationEventHandler> logger,
        IReceivedIntegrationEventRepository receivedIntegrationEventRepository,
        IReadOnlyDictionary<string, IIntegrationEventProcessor> integrationEventProcessors,
        IDatabaseConnectionFactory databaseConnectionFactory)
    {
        _logger = logger;
        _receivedIntegrationEventRepository = receivedIntegrationEventRepository;
        _integrationEventProcessors = integrationEventProcessors;
        _databaseConnectionFactory = databaseConnectionFactory;
    }

    public async Task HandleAsync(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var stopWatch = Stopwatch.StartNew();
        _integrationEventProcessors.TryGetValue(integrationEvent.EventName, out var integrationEventMapper);

        if (integrationEventMapper is null)
        {
            return;
        }

        using var connection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(CancellationToken.None)
            .ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
        try
        {
            var addResult = await _receivedIntegrationEventRepository
                .AddIfNotExistsAsync(integrationEvent.EventIdentification, integrationEvent.EventName, connection, transaction)
                .ConfigureAwait(false);

            if (addResult != AddReceivedIntegrationEventResult.EventRegistered)
            {
                stopWatch.Stop();
                _logger.LogWarning(
                    "Integration event \"{EventIdentification}\" with event type \"{EventType}\" wasn't registered successfully. Registration result: {RegisterIntegrationEventResult} in {ElapsedMilliseconds} ms",
                    integrationEvent.EventIdentification,
                    integrationEvent.EventName,
                    addResult.ToString(),
                    stopWatch.ElapsedMilliseconds);
                return;
            }

            await integrationEventMapper
                .ProcessAsync(integrationEvent, CancellationToken.None)
                .ConfigureAwait(false);

            transaction.Commit();
            stopWatch.Stop();
            _logger.LogInformation(
                "Processed integration event \"{EventIdentification}\" with event type \"{EventType}\" in {ElapsedMilliseconds} ms",
                integrationEvent.EventIdentification,
                integrationEvent.EventName,
                stopWatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            stopWatch.Stop();
            _logger.LogError(
                exception,
                "Failed to process integration event \"{EventIdentification}\" with event type \"{EventType}\" in {ElapsedMilliseconds} ms",
                integrationEvent.EventIdentification,
                integrationEvent.EventName,
                stopWatch.ElapsedMilliseconds);
            transaction.Rollback();
            throw;
        }
    }
}
