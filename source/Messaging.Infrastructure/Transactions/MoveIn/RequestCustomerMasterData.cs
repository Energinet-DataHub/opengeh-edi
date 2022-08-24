using System.Threading.Tasks;
using Messaging.Application.Transactions.MoveIn;

namespace Messaging.Infrastructure.Transactions.MoveIn;

public class RequestCustomerMasterData : IRequestCustomerMasterData
{
    private readonly IRequestDispatcher<FetchCustomerMasterData> _requestDispatcher;

    public RequestCustomerMasterData(IRequestDispatcher<FetchCustomerMasterData> requestDispatcher)
    {
        _requestDispatcher = requestDispatcher;
    }

    public async Task RequestMasterDataForAsync(FetchCustomerMasterData fetchMeteringPointMasterData)
    {
        await _requestDispatcher.SendAsync(fetchMeteringPointMasterData).ConfigureAwait(false);
    }
}
