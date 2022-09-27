using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.MasterData.MarketEvaluationPoints;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.MasterData.MarketEvaluationPoints;

public class CreateMarketEvaluationPointTests
    : TestBase, IAsyncLifetime
{
    public CreateMarketEvaluationPointTests(DatabaseFixture databaseFixture)
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

    [Fact]
    public async Task Energy_supplier_number_is_persisted()
    {
        var command = new CreateMarketEvalationPoint(
            SampleData.MarketEvaluationPointNumber,
            energySupplierNumber: SampleData.EnergySupplierNumber);
        await InvokeCommandAsync(command).ConfigureAwait(false);

        var dbContext = GetService<B2BContext>();
        var returned = dbContext.MarketEvaluationPoints.FirstOrDefault(x =>
            x.MarketEvaluationPointNumber == SampleData.MarketEvaluationPointNumber);

        Assert.Equal(returned?.EnergySupplierNumber, SampleData.EnergySupplierNumber);
    }

    [Fact]
    public async Task Handler_can_handle_both_requests()
    {
        var meteringPointCreatedCommand = new CreateMarketEvalationPoint(
            SampleData.MarketEvaluationPointNumber,
            Transactions.MoveIn.SampleData.IdOfGridOperatorForMeteringPoint.ToString());
        await InvokeCommandAsync(meteringPointCreatedCommand).ConfigureAwait(false);

        var energySupplierChangedCommand = new CreateMarketEvalationPoint(
            SampleData.MarketEvaluationPointNumber,
            energySupplierNumber: SampleData.EnergySupplierNumber);
        await InvokeCommandAsync(energySupplierChangedCommand).ConfigureAwait(false);

        var dbContext = GetService<B2BContext>();
        var returned = dbContext.MarketEvaluationPoints.FirstOrDefault(x =>
            x.MarketEvaluationPointNumber == SampleData.MarketEvaluationPointNumber);

        Assert.Equal(returned?.EnergySupplierNumber, SampleData.EnergySupplierNumber);
        Assert.Equal(returned?.GridOperatorId, Transactions.MoveIn.SampleData.IdOfGridOperatorForMeteringPoint);
    }
}
