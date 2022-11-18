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
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Application.Transactions.MoveIn;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.OutgoingMessages;

public class WhenOutgoingMessagesAreCreatedTests : TestBase
{
    public WhenOutgoingMessagesAreCreatedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task A_bundle_id_is_assigned()
    {
        await Scenario.Details(
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
            GetService<B2BContext>()).IsEffective().BuildAsync().ConfigureAwait(false);

        AssertOutgoingMessage.OutgoingMessage(
            SampleData.TransactionId,
            DocumentType.ConfirmRequestChangeOfSupplier.Name,
            ProcessType.MoveIn.Code,
            GetService<IDbConnectionFactory>());
    }
}
