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
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.OutgoingMessages;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

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

        var customerMasterDataMessage = await GetMessageAsync("CharacteristicsOfACustomerAtAnAP")
            .ConfigureAwait(false);
        Assert.NotNull(customerMasterDataMessage);
        Assert.Equal(ProcessType.MoveIn.Code, customerMasterDataMessage.ProcessType);
        Assert.Equal(SampleData.NewEnergySupplierNumber, customerMasterDataMessage.ReceiverId);
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

    private Task GivenMoveInHasBeenAcceptedAsync()
    {
        var message = new IncomingMessageBuilder()
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId)
            .WithMarketEvaluationPointId(SampleData.MarketEvaluationPointId)
            .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
            .Build();
        return InvokeCommandAsync(message);
    }

    private async Task<OutgoingMessage> GetMessageAsync(string documentType)
    {
        var connectionFactory = GetService<IDbConnectionFactory>();
        var outgoingMessage = await connectionFactory
            .GetOpenConnection()
            .QuerySingleAsync<OutgoingMessage>(
            $"SELECT [DocumentType], [ReceiverId], [CorrelationId], [OriginalMessageId], [ProcessType], [ReceiverRole], [SenderId], [SenderRole], [MarketActivityRecordPayload],[ReasonCode] FROM b2b.OutgoingMessages WHERE DocumentType = @DocumentType",
            new
            {
                DocumentType = documentType,
            }).ConfigureAwait(false);

        return outgoingMessage;
    }
}
