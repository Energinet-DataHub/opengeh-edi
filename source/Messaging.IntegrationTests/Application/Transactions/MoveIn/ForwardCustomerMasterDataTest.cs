using System.Threading.Tasks;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData;
using Messaging.Application.Transactions.MoveIn;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class ForwardCustomerMasterDataTest : TestBase
{
    public ForwardCustomerMasterDataTest(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task CustomerMasterDataIsForwardedToTheNewEnergySupplierAsync()
    {
        await SetupAnAcceptedMoveInTransactionAsync().ConfigureAwait(false);

        var forwardMeteringPointMasterData = new ForwardCustomerMasterData(SampleData.TransactionId, CreateMasterDataContent());
        await InvokeCommandAsync(forwardMeteringPointMasterData).ConfigureAwait(false);

        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>())
            .MeteringPointMasterDataWasSent();
    }

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId)
            .WithMarketEvaluationPointId(SampleData.MarketEvaluationPointId);
    }

    private static CustomerMasterDataContent CreateMasterDataContent()
    {
        return new CustomerMasterDataContent(
            SampleData.MarketEvaluationPointId,
            SampleData.ElectricalHeating,
            SampleData.ElectricalHeatingStart,
            SampleData.SecondCustomerId,
            SampleData.SecondCustomerName,
            SampleData.SecondCustomerId,
            SampleData.SecondCustomerName,
            SampleData.ProtectedName,
            SampleData.HasEnergySupplier,
            SampleData.SupplyStart,
            SampleData.UsagePointLocations);
    }

    private async Task SetupAnAcceptedMoveInTransactionAsync()
    {
        await InvokeCommandAsync(MessageBuilder().Build()).ConfigureAwait(false);
    }
}
