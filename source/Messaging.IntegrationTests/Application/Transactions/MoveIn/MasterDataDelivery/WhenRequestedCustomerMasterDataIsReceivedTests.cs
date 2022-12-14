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
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn.MasterDataDelivery;

public class WhenRequestedCustomerMasterDataIsReceivedTests
    : TestBase, IAsyncLifetime
{
    public WhenRequestedCustomerMasterDataIsReceivedTests(DatabaseFixture databaseFixture)
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
    public async Task Current_known_customer_master_data_is_stored_in_transaction()
    {
        var command = CreateCommand();
        await InvokeCommandAsync(command).ConfigureAwait(false);

        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>(), GetService<ISerializer>())
            .HasCustomerMasterData(ParseFrom(command.Data));
    }

    [Fact]
    public async Task Current_known_customer_master_data_message_is_created_for_the_energy_supplier()
    {
        var command = CreateCommand();
        await InvokeCommandAsync(command).ConfigureAwait(false);

        var assertMessage = AssertOutgoingMessage();
        assertMessage.HasReceiverId(SampleData.NewEnergySupplierNumber);
        assertMessage.HasReceiverRole(MarketRole.EnergySupplier.ToString());
        assertMessage.HasSenderId(DataHubDetails.IdentificationNumber.Value);
        assertMessage.HasSenderRole(MarketRole.MeteringPointAdministrator.ToString());
        assertMessage.WithMarketActivityRecord()
            .HasOriginalTransactionId(SampleData.TransactionId)
            .HasValidityStart(SampleData.SupplyStart)
            .HasMarketEvaluationPointValue(nameof(MarketEvaluationPoint.MarketEvaluationPointId), SampleData.MeteringPointNumber)
            .HasMarketEvaluationPointDateValue(nameof(MarketEvaluationPoint.SupplyStart), SampleData.SupplyStart)
            .HasMarketEvaluationPointValue(nameof(MarketEvaluationPoint.ElectricalHeating), SampleData.ElectricalHeating)
            .HasMarketEvaluationPointDateValue(nameof(MarketEvaluationPoint.ElectricalHeatingStart), SampleData.ElectricalHeatingStart)
            .HasMarketEvaluationPointValue(nameof(MarketEvaluationPoint.HasEnergySupplier), SampleData.HasEnergySupplier)
            .HasMarketEvaluationPointValue(nameof(MarketEvaluationPoint.ProtectedName), SampleData.ProtectedName)
            .HasMarketEvaluationPointValue($"{nameof(MarketEvaluationPoint.FirstCustomerId)}.{nameof(MarketEvaluationPoint.FirstCustomerId.Id)}", SampleData.ConsumerId)
            .HasMarketEvaluationPointValue($"{nameof(MarketEvaluationPoint.FirstCustomerId)}.{nameof(MarketEvaluationPoint.FirstCustomerId.CodingScheme)}", SampleData.ConsumerIdType)
            .HasMarketEvaluationPointValue(nameof(MarketEvaluationPoint.FirstCustomerName), SampleData.ConsumerName)
            .HasMarketEvaluationPointValue($"{nameof(MarketEvaluationPoint.SecondCustomerId)}.{nameof(MarketEvaluationPoint.SecondCustomerId.Id)}", SampleData.ConsumerId)
            .HasMarketEvaluationPointValue($"{nameof(MarketEvaluationPoint.SecondCustomerId)}.{nameof(MarketEvaluationPoint.SecondCustomerId.CodingScheme)}", SampleData.ConsumerIdType)
            .HasMarketEvaluationPointValue(nameof(MarketEvaluationPoint.SecondCustomerName), SampleData.ConsumerName)
            .NotEmpty(nameof(MarketActivityRecord.Id));
    }

    private static SetCurrentKnownCustomerMasterData CreateCommand()
    {
        var customerMasterData = new CustomerMasterDataContent(
            SampleData.MeteringPointNumber,
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
        return new SetCurrentKnownCustomerMasterData(SampleData.TransactionId, customerMasterData);
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

    private AssertOutgoingMessage AssertOutgoingMessage()
    {
        var assertMessage = Assertions.AssertOutgoingMessage.OutgoingMessage(
            SampleData.TransactionId,
            MessageType.CharacteristicsOfACustomerAtAnAP.Name,
            ProcessType.MoveIn.Code,
            GetService<IDbConnectionFactory>());
        return assertMessage;
    }
}
