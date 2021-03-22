using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketData.Infrastructure.Outbox
{
    /// <summary>
    /// Interface for the internal command repository
    /// </summary>
    public interface IInternalCommandRepository
    {
        /// <summary>
        /// Fetches all unprocessed commands from the database
        /// </summary>
        Task<IEnumerable<InternalCommand>> GetUnprocessedInternalCommandsAsync();
    }
}
