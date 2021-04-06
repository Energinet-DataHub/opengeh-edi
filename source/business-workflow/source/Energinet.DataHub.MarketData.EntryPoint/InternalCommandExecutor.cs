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
using Energinet.DataHub.MarketData.Infrastructure.InternalCommand;
using Microsoft.Azure.WebJobs;

namespace Energinet.DataHub.MarketData.EntryPoint
{
    public class InternalCommandExecutor
    {
        private readonly IInternalCommandService _internalCommandService;

        public InternalCommandExecutor(IInternalCommandService internalCommandService)
        {
            _internalCommandService = internalCommandService;
        }

        [FunctionName("InternalCommandDispatcher")]
        public async Task RunAsync([TimerTrigger("%INTERNAL_COMMAND_DISPATCH_TRIGGER_TIMER%")] TimerInfo timer)
        {
            await _internalCommandService.ExecuteUnprocessedInternalCommandsAsync().ConfigureAwait(false);
        }
    }
}
