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
        /// Fetches the next unprocessed internal command
        /// </summary>
        Task<InternalCommand?> GetUnprocessedInternalCommandAsync();
    }
}
