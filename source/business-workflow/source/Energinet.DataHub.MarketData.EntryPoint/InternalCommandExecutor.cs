using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Infrastructure.InternalCommand;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

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
