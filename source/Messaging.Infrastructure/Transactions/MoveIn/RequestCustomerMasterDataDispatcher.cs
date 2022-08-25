using System.Threading.Tasks;
using Messaging.Application.Transactions.MoveIn;

namespace Messaging.Infrastructure.Transactions.MoveIn;

public class RequestCustomerMasterDataDispatcher : IRequestDispatcher<FetchCustomerMasterData>
{
    public Task SendAsync(FetchCustomerMasterData fetchMeteringPointMasterData)
    {
        throw new System.NotImplementedException();
    }
}
