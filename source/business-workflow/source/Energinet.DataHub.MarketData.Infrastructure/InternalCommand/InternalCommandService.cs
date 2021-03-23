using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using Microsoft.Azure.WebJobs;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public class InternalCommandService : IInternalCommandService
    {
        private readonly IInternalCommandRepository _internalCommandRepository;

        public InternalCommandService(IInternalCommandRepository internalCommandRepository)
        {
            _internalCommandRepository = internalCommandRepository;
        }

        public async Task GetUnprocessedInternalCommandsInBatchesAsync(
            IAsyncCollector<dynamic> internalCommandServiceBus, int id)
        {
            var commands = await _internalCommandRepository.GetUnprocessedInternalCommandsInBatchesAsync(id)
                .ConfigureAwait(false);
            var internalCommands = commands.ToList();

            var lastId = internalCommands.Last().Id;

            await Task.WhenAll(internalCommands.Select(command => internalCommandServiceBus.AddAsync(command)).ToArray());

            if (internalCommands.Count > 0)
            {
                await GetUnprocessedInternalCommandsInBatchesAsync(internalCommandServiceBus, lastId);
            }
        }

        public async Task ExecuteInternalCommandAsync(InternalCommand internalCommand)
        {
            // TODO: Forward the command to the command handler

            // Set the command as processed
            await _internalCommandRepository.ProcessInternalCommandAsync(internalCommand.Id).ConfigureAwait(false);
        }
    }
}
