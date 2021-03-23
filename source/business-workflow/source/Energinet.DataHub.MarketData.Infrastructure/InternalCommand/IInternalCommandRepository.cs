using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    /// <summary>
    /// Interface for the internal command repository
    /// </summary>
    public interface IInternalCommandRepository
    {
        /// <summary>
        /// Fetches all unprocessed commands from the database in batches of 100
        /// </summary>
        Task<IEnumerable<InternalCommand>> GetUnprocessedInternalCommandsInBatchesAsync(int id);

        /// <summary>
        /// Processes the InternalCommand as read
        /// </summary>
        /// <param name="id"></param>
        Task ProcessInternalCommandAsync(int id);
    }
}
