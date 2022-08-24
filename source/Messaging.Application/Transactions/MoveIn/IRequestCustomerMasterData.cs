using System.Threading.Tasks;

namespace Messaging.Application.Transactions.MoveIn;

/// <summary>
/// Interface for fetching customer master data
/// </summary>
public interface IRequestCustomerMasterData
{
    /// <summary>
    /// Request master data for a customer
    /// </summary>
    /// <param name="fetchMeteringPointMasterData"></param>
    /// <returns><see cref="Task"/></returns>
    Task RequestMasterDataForAsync(FetchCustomerMasterData fetchMeteringPointMasterData);
}
