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

using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketData.EntryPoint
{
    public class ActorMessageDispatcher
    {
        private readonly IForwardMessageService _forwardMessageService;

        public ActorMessageDispatcher(IForwardMessageService forwardMessageService)
        {
            _forwardMessageService = forwardMessageService;
        }

        [FunctionName("MessageDispatcher")]
        public async Task RunAsync(
            [TimerTrigger("%ACTOR_MESSAGE_DISPATCH_TRIGGER_TIMER%")] TimerInfo timer)
        {
            _ = timer; // Fix for the unused parameter but the TimerTrigger attribute is still needed https://github.com/dotnet/roslyn-analyzers/issues/2589
            await _forwardMessageService.ProcessMessagesAsync().ConfigureAwait(false);
        }
    }
}
