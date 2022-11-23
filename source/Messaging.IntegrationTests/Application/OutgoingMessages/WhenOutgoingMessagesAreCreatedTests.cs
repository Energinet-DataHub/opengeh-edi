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
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.OutgoingMessages;

public class WhenOutgoingMessagesAreCreatedTests : TestBase, IAsyncLifetime
{
    public WhenOutgoingMessagesAreCreatedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    public Task InitializeAsync()
    {
        return GetService<IDbConnectionFactory>().GetOpenConnection().ExecuteAsync(
            $"DROP TABLE IF EXISTS  [B2B].ActorMessageQueue_{SampleData.NewEnergySupplierNumber}");
    }

    public Task DisposeAsync()
    {
        return GetService<IDbConnectionFactory>().GetOpenConnection().ExecuteAsync(
            $"DROP TABLE IF EXISTS [B2B].ActorMessageQueue_{SampleData.NewEnergySupplierNumber}");
    }

    [Fact]
    public async Task Outgoing_message_is_enqueued()
    {
        await GivenRequestHasBeenAccepted().ConfigureAwait(false);

        var sql = $"SELECT * FROM [B2B].[ActorMessageQueue_{SampleData.NewEnergySupplierNumber}]";
        var result = await GetService<IDbConnectionFactory>()
            .GetOpenConnection()
            .QuerySingleOrDefaultAsync(sql)
            .ConfigureAwait(false);

        Assert.NotNull(result);
        Assert.Equal(result.DocumentType, DocumentType.ConfirmRequestChangeOfSupplier.Name);
        Assert.Equal(result.ReceiverId, SampleData.NewEnergySupplierNumber);
        Assert.Equal(result.ReceiverRole, MarketRole.EnergySupplier.Name);
        Assert.Equal(result.SenderId, DataHubDetails.IdentificationNumber.Value);
        Assert.Equal(result.SenderRole, MarketRole.MeteringPointAdministrator.Name);
        Assert.Equal(result.ProcessType, ProcessType.MoveIn.Code);
        Assert.NotNull(result.Payload);
    }

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId);
    }

    private async Task GivenRequestHasBeenAccepted()
    {
        var incomingMessage = MessageBuilder()
            .WithProcessType(ProcessType.MoveIn.Code)
            .WithReceiver(SampleData.ReceiverId)
            .WithSenderId(SampleData.SenderId)
            .WithConsumerName(SampleData.ConsumerName)
            .Build();

        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
    }
}
