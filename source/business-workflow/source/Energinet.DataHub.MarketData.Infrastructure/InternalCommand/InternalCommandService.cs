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
        private readonly IInternalCommandQuerySettings _querySettings;

        public InternalCommandService(IInternalCommandRepository internalCommandRepository, IInternalCommandQuerySettings querySettings)
        {
            _internalCommandRepository = internalCommandRepository;
            _querySettings = querySettings;
        }

        public async Task GetUnprocessedInternalCommandsInBatchesAsync(
            IAsyncCollector<dynamic> internalCommandServiceBus, int id)
        {
            var commands = await _internalCommandRepository.GetUnprocessedInternalCommandsInBatchesAsync(id)
                .ConfigureAwait(false);
            var internalCommands = commands.ToList();

            var lastId = internalCommands.Last().Id;

            await Task.WhenAll(internalCommands.Select(command => internalCommandServiceBus.AddAsync(command)).ToArray());

            // If we're at the max batch size that means that there might be more unprocessed commands in the DB and we need to do another round
            if (internalCommands.Count == _querySettings.BatchSize)
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
