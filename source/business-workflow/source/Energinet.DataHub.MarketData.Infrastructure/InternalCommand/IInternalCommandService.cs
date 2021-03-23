using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    /// <summary>
    /// swdwa
    /// </summary>
    public interface IInternalCommandService
    {
        /// <summary>
        /// Gets the unprocessed internal commands in batches of 100
        /// </summary>
        /// <param name="internalCommandServiceBus"></param>
        /// <param name="id"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task GetUnprocessedInternalCommandsInBatchesAsync(IAsyncCollector<dynamic> internalCommandServiceBus, int id = 0);
    }
}
