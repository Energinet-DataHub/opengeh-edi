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
        /// TODO: WRITE SOMETHING HERE
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task ExecuteUnprocessedInternalCommandsAsync();
    }
}
