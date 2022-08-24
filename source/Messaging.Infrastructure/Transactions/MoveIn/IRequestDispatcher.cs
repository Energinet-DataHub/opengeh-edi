using System.Threading.Tasks;

namespace Messaging.Infrastructure.Transactions.MoveIn;

/// <summary>
/// Interface for a request dispatcher
/// </summary>
/// <typeparam name="T">Type of request to dispatch</typeparam>
public interface IRequestDispatcher<in T>
{
    /// <summary>
    /// Dispatch a request async
    /// </summary>
    /// <param name="fetchMeteringPointMasterData"></param>
    /// <returns><see cref="Task"/></returns>
    public Task SendAsync(T fetchMeteringPointMasterData);
}
