using System.Threading.Tasks;

namespace Messaging.Application.Transactions.MoveIn
{
    /// <summary>
    /// Interface for move in request adapter
    /// </summary>
    public interface IMoveInRequestAdapter
    {
        /// <summary>
        /// Invokes a move in business process asynchronously
        /// </summary>
        /// <param name="request"></param>
        /// <returns><see cref="Task"/></returns>
        Task<BusinessRequestResult> InvokeAsync(MoveInRequest request);
    }
}
