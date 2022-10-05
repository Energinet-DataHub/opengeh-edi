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

using System.Threading.Tasks;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;
using MarketActivityRecord = Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp.MarketActivityRecord;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class ForwardCustomerMasterDataTests : TestBase
{
    public ForwardCustomerMasterDataTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Customer_master_data_is_marked_as_sent_on_transaction()
    {
        await GivenMoveInHasBeenAcceptedAsync().ConfigureAwait(false);

        var command = new ForwardCustomerMasterData(SampleData.TransactionId, CreateMasterDataContent());
        await InvokeCommandAsync(command).ConfigureAwait(false);

        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>())
            .CustomerMasterDataWasSent();
    }

    [Fact]
    public async Task Outgoing_message_is_created()
    {
        await GivenMoveInHasBeenAcceptedAsync().ConfigureAwait(false);

        var command = new ForwardCustomerMasterData(SampleData.TransactionId, CreateMasterDataContent());
        await InvokeCommandAsync(command).ConfigureAwait(false);

        var assertMessage = AssertOutgoingMessage();
        assertMessage.HasReceiverId(SampleData.NewEnergySupplierNumber);
        assertMessage.HasReceiverRole(MarketRoles.EnergySupplier.ToString());
        assertMessage.HasSenderId(DataHubDetails.IdentificationNumber.Value);
        assertMessage.HasSenderRole(MarketRoles.MeteringPointAdministrator.ToString());
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

    private static CustomerMasterDataContent CreateMasterDataContent()
    {
        return new CustomerMasterDataContent(
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
            SampleData.UsagePointLocations);
    }

    private Task GivenMoveInHasBeenAcceptedAsync()
    {
        var message = new IncomingMessageBuilder()
            .WithSenderId(SampleData.SenderId)
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId)
            .WithMarketEvaluationPointId(SampleData.MarketEvaluationPointId)
            .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
            .WithEffectiveDate(SampleData.SupplyStart)
            .WithConsumerId(SampleData.ConsumerId)
            .WithConsumerName(SampleData.ConsumerName)
            .Build();
        return InvokeCommandAsync(message);
    }

    private AssertOutgoingMessage AssertOutgoingMessage()
    {
        var assertMessage = Assertions.AssertOutgoingMessage.OutgoingMessage(
            SampleData.OriginalMessageId,
            DocumentType.CharacteristicsOfACustomerAtAnAP.Name,
            ProcessType.MoveIn.Code,
            GetService<IDbConnectionFactory>());
        return assertMessage;
    }
}
