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
using Energinet.DataHub.MarketRoles.EntryPoints.InternalCommandDispatcher.Infrastructure.TimerTriggers;
using Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.InternalCommandDispatcher
{
    public class Dispatcher
    {
        private readonly IInternalCommandProcessor _internalCommandProcessor;

        public Dispatcher(IInternalCommandProcessor internalCommandProcessor)
        {
            _internalCommandProcessor = internalCommandProcessor ?? throw new ArgumentNullException(nameof(internalCommandProcessor));
        }

        [Function("Dispatcher")]
        public Task RunAsync([TimerTrigger("%DISPATCH_TRIGGER_TIMER%")] TimerInfo timerTimerInfo, FunctionContext context)
        {
            var logger = context.GetLogger("Dispatcher");
            logger.LogInformation($"Timer trigger function executed at: {DateTime.Now}");
            logger.LogInformation($"Next timer schedule at: {timerTimerInfo.ScheduleStatus?.Next}");

            return _internalCommandProcessor.ProcessUndispatchedAsync();
        }
    }
}
