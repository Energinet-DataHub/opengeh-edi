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
using Messaging.Application.Transactions.UpdateCustomer;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.UpdateCustomer;

public class WhenRequestIsReceivedTests : TestBase
{
    public WhenRequestIsReceivedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Update_customer_master_data_transaction_is_started()
    {
        var command = new UpdateCustomerMasterData(SampleData.MarketEvaluationPointNumber, SampleData.EffectiveDate, SampleData.TransactionId);

        await InvokeCommandAsync(command).ConfigureAwait(false);

        await AssertTransaction.TransactionAsync(SampleData.TransactionId, GetService<IEdiDatabaseConnection>()).ConfigureAwait(false);
    }
}
