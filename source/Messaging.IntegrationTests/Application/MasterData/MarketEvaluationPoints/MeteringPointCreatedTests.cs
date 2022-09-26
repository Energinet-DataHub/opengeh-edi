using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.MasterData.MarketEvaluationPoints;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Messaging.IntegrationTests.Application.MasterData.MarketEvaluationPoints;

public class MeteringPointCreatedTests
    : TestBase, IAsyncLifetime
{
    public MeteringPointCreatedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Grid_operator_id_is_persisted()
    {
        var command = new CreateMarketEvalationPoint(
            SampleData.MarketEvaluationPointNumber,
            Transactions.MoveIn.SampleData.IdOfGridOperatorForMeteringPoint.ToString());
        await InvokeCommandAsync(command).ConfigureAwait(false);

        var dbContext = GetService<B2BContext>();
        var returned = dbContext.MarketEvaluationPoints.FirstOrDefault(x
            => x.MarketEvaluationPointNumber == SampleData.MarketEvaluationPointNumber);

        Assert.Equal(returned?.GridOperatorId, Transactions.MoveIn.SampleData.IdOfGridOperatorForMeteringPoint);
    }
}
