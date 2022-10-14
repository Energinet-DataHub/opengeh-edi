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
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn.MasterDataDelivery;

public class SendCustomerMasterDataToGridOperatorTests
    : TestBase, IAsyncLifetime
{
    public SendCustomerMasterDataToGridOperatorTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    public Task InitializeAsync()
    {
        return Scenario.Details(
                SampleData.TransactionId,
                SampleData.MeteringPointNumber,
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
            .CustomerMasterDataIsReceived(
                SampleData.MeteringPointNumber,
                SampleData.ElectricalHeating,
                SampleData.ElectricalHeatingStart,
                SampleData.ConsumerId,
                SampleData.ConsumerName,
                SampleData.ConsumerId,
                SampleData.ConsumerName,
                SampleData.ProtectedName,
                SampleData.HasEnergySupplier,
                SampleData.SupplyStart)
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
    public async Task Message_is_delivered()
    {
        await InvokeCommandAsync(new SendCustomerMasterDataToGridOperator(SampleData.TransactionId)).ConfigureAwait(false);

        var assertMessage = AssertOutgoingMessage();
        assertMessage.HasReceiverId(SampleData.NumberOfGridOperatorForMeteringPoint);
        assertMessage.HasReceiverRole(MarketRole.GridOperator.ToString());
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
            .HasMarketEvaluationPointValue($"{nameof(MarketEvaluationPoint.UsagePointLocation)}[0].Type", "D01")
            .HasMarketEvaluationPointValue($"{nameof(MarketEvaluationPoint.UsagePointLocation)}[1].Type", "D04")
            .NotEmpty(nameof(MarketActivityRecord.Id));
        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>())
            .HasCustomerMasterDataSentToGridOperatorState(MoveInTransaction.MasterDataState.Sent);
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
