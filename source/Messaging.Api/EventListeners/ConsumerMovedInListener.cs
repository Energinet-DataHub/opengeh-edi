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
using System.Threading.Tasks;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.EventListeners;

public class ConsumerMovedInListener
{
    private readonly ILogger<ConsumerMovedInListener> _logger;
    private readonly ICommandScheduler _commandScheduler;
    private readonly IUnitOfWork _unitOfWork;

    public ConsumerMovedInListener(ILogger<ConsumerMovedInListener> logger, ICommandScheduler commandScheduler, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _commandScheduler = commandScheduler;
        _unitOfWork = unitOfWork;
    }

    [Function("ConsumerMovedInListener")]
    public async Task RunAsync(
        [ServiceBusTrigger("consumer-moved-in", "consumer-moved-in", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_INTEGRATION_EVENTS_LISTENER")] byte[] data,
        FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var consumerMovedIn = Contracts.IntegrationEvents.ConsumerMovedIn.Parser.ParseFrom(data);
        _logger.LogInformation($"Received consumer moved in event: {consumerMovedIn}");
        await _commandScheduler.EnqueueAsync(new CompleteMoveInTransaction(consumerMovedIn.ProcessId))
            .ConfigureAwait(false);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);
    }
}
