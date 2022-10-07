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
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.IntegrationTests.Fixtures;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn.MasterDataDelivery;

public class ReceiveCustomerMasterDataTests
    : TestBase, IAsyncLifetime
{
    public ReceiveCustomerMasterDataTests(DatabaseFixture databaseFixture)
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
            .BuildAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Customer_master_data_is_stored()
    {
        var command = CreateCommand();
        await InvokeCommandAsync(command).ConfigureAwait(false);

        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>(), GetService<ISerializer>())
            .HasCustomerMasterData(ParseFrom(command.Data));
    }

    private static ReceiveCustomerMasterData CreateCommand()
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
        return new ReceiveCustomerMasterData(SampleData.TransactionId, customerMasterData);
    }

    private static CustomerMasterData ParseFrom(CustomerMasterDataContent data)
    {
        return new CustomerMasterData(
            marketEvaluationPoint: data.MarketEvaluationPoint,
            electricalHeating: data.ElectricalHeating,
            electricalHeatingStart: data.ElectricalHeatingStart,
            firstCustomerId: data.FirstCustomerId,
            firstCustomerName: data.FirstCustomerName,
            secondCustomerId: data.SecondCustomerId,
            secondCustomerName: data.SecondCustomerName,
            protectedName: data.ProtectedName,
            hasEnergySupplier: data.HasEnergySupplier,
            supplyStart: data.SupplyStart);
    }
}
