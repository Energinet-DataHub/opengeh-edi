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
using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.GenericNotification;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests.Transactions.MoveIn;

public class CompleteMoveInTests : TestBase
{
    private readonly MarketEvaluationPointProviderStub _marketEvaluationPointProvider;

    public CompleteMoveInTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _marketEvaluationPointProvider = (MarketEvaluationPointProviderStub)GetService<IMarketEvaluationPointProvider>();
    }

    [Fact]
    public async Task Transaction_must_exist()
    {
        var processId = "Not existing";
        var command = new CompleteMoveInTransaction(processId);

        await Assert.ThrowsAsync<TransactionNotFoundException>(() => InvokeCommandAsync(command)).ConfigureAwait(false);
    }

    [Fact]
    public async Task Current_energy_supplier_is_notified_when_consumer_move_in_is_completed()
    {
        var marketEvaluationPoint = new MarketEvaluationPoint(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        _marketEvaluationPointProvider.Add(marketEvaluationPoint);

        var transaction = new MoveInTransaction(
            Guid.NewGuid().ToString(),
            marketEvaluationPoint.GsrnNumber,
            SystemClock.Instance.GetCurrentInstant(),
            marketEvaluationPoint.GlnNumberOfEnergySupplier);
        transaction.Start(BusinessRequestResult.Succeeded(Guid.NewGuid().ToString()));
        GetService<IMoveInTransactionRepository>().Add(transaction);
        await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);

        var completeCommand = new CompleteMoveInTransaction(transaction.ProcessId!);
        await InvokeCommandAsync(completeCommand).ConfigureAwait(false);

        var context = GetService<B2BContext>();
        var message = context.OutgoingMessages.FirstOrDefault(m => m.DocumentType == "GenericNotification" && m.ProcessType == "E01");

        Assert.NotNull(message);
        var extractedMessage = GetService<IMarketActivityRecordParser>()
            .From<MarketActivityRecord>(message!.MarketActivityRecordPayload);
        Assert.Equal(transaction.CurrentEnergySupplierId, message.ReceiverId);
        Assert.Equal(MarketRoles.EnergySupplier, message.ReceiverRole);
        Assert.Equal(DataHubDetails.IdentificationNumber, message.SenderId);
        Assert.Equal(MarketRoles.MeteringPointAdministrator, message.SenderRole);
        Assert.Equal("E01", message.ProcessType);
        Assert.Null(message.ReasonCode);
        Assert.Equal("GenericNotification", message.DocumentType);
    }
}
