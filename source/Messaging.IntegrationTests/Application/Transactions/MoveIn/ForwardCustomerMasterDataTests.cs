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
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.MasterData;
using Messaging.Application.Transactions.MoveIn;
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
    public async Task Customer_master_data_is_sent_to_the_new_energy_supplier()
    {
        await SetupAnAcceptedMoveInTransactionAsync().ConfigureAwait(false);

        var forwardMeteringPointMasterData = new ForwardCustomerMasterData(SampleData.TransactionId, CreateMasterDataContent());
        await InvokeCommandAsync(forwardMeteringPointMasterData).ConfigureAwait(false);

        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>())
            .MeteringPointMasterDataWasSent();
    }

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId)
            .WithMarketEvaluationPointId(SampleData.MarketEvaluationPointId);
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

    private async Task SetupAnAcceptedMoveInTransactionAsync()
    {
        await InvokeCommandAsync(MessageBuilder().Build()).ConfigureAwait(false);
    }
}
