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
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn.MasterDataDelivery;

public class WhenCustomerMasterDataIsReceivedTests : TestBase, IAsyncLifetime
{
    public WhenCustomerMasterDataIsReceivedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    public Task InitializeAsync()
    {
        return Scenario.Details(
                SampleData.TransactionId,
                SampleData.MarketEvaluationPointId,
                SampleData.SupplyStart,
                SampleData.CurrentEnergySupplierNumber,
                SampleData.NewEnergySupplierNumber,
                SampleData.ConsumerId,
                SampleData.ConsumerIdType,
                SampleData.ConsumerName,
                SampleData.OriginalMessageId,
                GetService<IMediator>(),
                GetService<B2BContext>())
            .IsEffective()
            .WithGridOperatorForMeteringPoint(
                SampleData.IdOfGridOperatorForMeteringPoint,
                SampleData.NumberOfGridOperatorForMeteringPoint)
            .CustomerMasterDataIsReceived(
                SampleData.MeteringPointNumber,
                SampleData.ElectricalHeating,
                SampleData.ElectricalHeatingStart,
                SampleData.ConsumerId,
                SampleData.ConsumerName,
                string.Empty,
                string.Empty,
                SampleData.ProtectedName,
                SampleData.HasEnergySupplier,
                SampleData.SupplyStart)
            .BuildAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Dispatch_of_customer_master_data_to_the_energy_supplier_is_scheduled()
    {
        await WhenCustomerMasterDataIsReceived().ConfigureAwait(false);

        AssertCommand<SendCustomerMasterDataToEnergySupplier>();
    }

    private async Task WhenCustomerMasterDataIsReceived()
    {
        var customerMasterData = new CustomerMasterDataContent(
            SampleData.MarketEvaluationPointId,
            SampleData.ElectricalHeating,
            SampleData.ElectricalHeatingStart,
            SampleData.ConsumerId,
            SampleData.ConsumerName,
            SampleData.ConsumerId,
            SampleData.ConsumerName,
            SampleData.ProtectedName,
            SampleData.HasEnergySupplier,
            SampleData.SupplyStart,
            Array.Empty<UsagePointLocation>());
        await InvokeCommandAsync(new ReceiveCustomerMasterData(SampleData.TransactionId, customerMasterData));
    }

    private AssertQueuedCommand AssertCommand<TCommand>()
    {
        return AssertQueuedCommand.QueuedCommand<TCommand>(
            GetService<IDbConnectionFactory>(),
            GetService<InternalCommandMapper>());
    }
}
