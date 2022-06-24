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
using JetBrains.Annotations;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class FetchMeteringPointMasterDataTests : TestBase
{
    public FetchMeteringPointMasterDataTests([NotNull] DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Fetch_metering_point_master_data_when_the_transaction_is_accepted()
    {
        await SetupMasterDataDetailsAsync().ConfigureAwait(false);
        var incomingMessage = MessageBuilder()
            .WithProcessType(ProcessType.MoveIn.Code)
            .WithReceiver("5790001330552")
            .WithSenderId("123456")
            .WithConsumerName("John Doe")
            .WithMarketEvaluationPointId(SampleData.MateringPointNumber)
            .Build();

        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);

        AssertQueuedCommand.QueuedCommand<FetchMeteringPointMasterData>(GetService<IDbConnectionFactory>());
    }

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId);
    }

    private async Task SetupMasterDataDetailsAsync()
    {
        GetService<IMarketEvaluationPointRepository>().Add(MarketEvaluationPoint.Create(SampleData.CurrentEnergySupplierNumber, SampleData.MateringPointNumber));
        await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
    }
}
