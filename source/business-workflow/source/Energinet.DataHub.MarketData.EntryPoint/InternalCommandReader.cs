using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Infrastructure.InternalCommand;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketData.EntryPoint
{
    public class InternalCommandReader
    {
        private readonly IInternalCommandService _internalCommandService;

        public InternalCommandReader(IInternalCommandService internalCommandService)
        {
            _internalCommandService = internalCommandService;
        }

        [FunctionName("InternalCommandReader")]
        public async Task RunAsync(
            [ServiceBusTrigger("commands", Connection = "INTERNAL_COMMAND_SERVICE_BUS_LISTENER")] string item)
        {
            await _internalCommandService.ExecuteInternalCommandAsync(JsonSerializer.Deserialize<InternalCommand>(item)).ConfigureAwait(false);
        }
    }
}
