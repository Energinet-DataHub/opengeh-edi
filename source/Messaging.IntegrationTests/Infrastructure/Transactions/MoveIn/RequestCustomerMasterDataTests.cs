using System.Threading.Tasks;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Transactions.MoveIn;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Transactions.MoveIn;

public class RequestCustomerMasterDataTests : TestBase
{
    private readonly RequestDispatcherSpy _requestDispatcherSpy;
    private readonly IRequestCustomerMasterData _requestCustomerMasterData;

    public RequestCustomerMasterDataTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _requestDispatcherSpy = new RequestDispatcherSpy();
        _requestCustomerMasterData = new RequestCustomerMasterData(_requestDispatcherSpy);
    }

    [Fact]
    public async Task Requests_are_dispatched()
    {
        var request = CreateRequest();
        await _requestCustomerMasterData.RequestMasterDataForAsync(request).ConfigureAwait(false);
        var dispatchedMessage = _requestDispatcherSpy.GetRequest(request.TransactionId);
        Assert.NotNull(dispatchedMessage);
    }

    private static FetchCustomerMasterData CreateRequest()
    {
        return new FetchCustomerMasterData(
            "FakeBusinessProcessId",
            "FakeMarketEvaluationPoint",
            "FakeTransactionId");
    }
}
