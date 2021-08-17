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
using Energinet.DataHub.MarketRoles.EntryPoints.Outbox.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox
{
    internal class OutboxWatcher
    {
        private readonly ILogger _logger;
        private readonly OutboxOrchestrator _outboxOrchestrator;

        public OutboxWatcher(
            ILogger logger,
            OutboxOrchestrator outboxOrchestrator)
        {
            _logger = logger;
            _outboxOrchestrator = outboxOrchestrator;
        }

        [Function("OutboxWatcher")]
        public async Task RunAsync(
            [TimerTrigger("%ACTOR_MESSAGE_DISPATCH_TRIGGER_TIMER%")] TimerInfo timerInformation)
        {
            _logger.LogInformation($"Timer trigger function executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {timerInformation?.ScheduleStatus?.Next}");

            await _outboxOrchestrator.ProcessOutboxMessagesAsync().ConfigureAwait(false);
        }
    }
}
