// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
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
        var command = new CreateMarketEvaluationPoint(
            SampleData.MarketEvaluationPointNumber,
            Guid.NewGuid().ToString(),
            gridOperatorId: Transactions.MoveIn.SampleData.IdOfGridOperatorForMeteringPoint);
        await InvokeCommandAsync(command).ConfigureAwait(false);

        var dbContext = GetService<B2BContext>();
        var returned = dbContext.MarketEvaluationPoints.FirstOrDefault(x
            => x.MarketEvaluationPointNumber == SampleData.MarketEvaluationPointNumber);

        Assert.Equal(returned?.GridOperatorId, SampleData.IdOfGridOperatorForMeteringPoint);
    }

    [Fact]
    public async Task Energy_supplier_number_is_persisted()
    {
        var command = new CreateMarketEvaluationPoint(
            SampleData.MarketEvaluationPointNumber,
            Guid.NewGuid().ToString(),
            Guid.Empty,
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
        var meteringPointId = Guid.NewGuid().ToString();

        var meteringPointCreatedCommand = new CreateMarketEvaluationPoint(
            SampleData.MarketEvaluationPointNumber,
            meteringPointId,
            gridOperatorId: SampleData.IdOfGridOperatorForMeteringPoint);
        await InvokeCommandAsync(meteringPointCreatedCommand).ConfigureAwait(false);

        var energySupplierChangedCommand = new CreateMarketEvaluationPoint(
            SampleData.MarketEvaluationPointNumber,
            meteringPointId,
            Guid.Empty,
            energySupplierNumber: SampleData.EnergySupplierNumber);
        await InvokeCommandAsync(energySupplierChangedCommand).ConfigureAwait(false);

        var dbContext = GetService<B2BContext>();
        var returned = dbContext.MarketEvaluationPoints.FirstOrDefault(x =>
            x.MarketEvaluationPointNumber == SampleData.MarketEvaluationPointNumber);

        Assert.Equal(returned?.EnergySupplierNumber, SampleData.EnergySupplierNumber);
        Assert.Equal(returned?.GridOperatorId, SampleData.IdOfGridOperatorForMeteringPoint);
    }
}
