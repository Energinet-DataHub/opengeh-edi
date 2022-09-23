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
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Application.Transactions.MoveIn.Notifications;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class WhenAConsumerHasMovedInTests : TestBase
{
    private readonly IMoveInTransactionRepository _transactionRepository;

    public WhenAConsumerHasMovedInTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _transactionRepository = GetService<IMoveInTransactionRepository>();
    }

    [Fact]
    public async Task An_exception_is_thrown_if_transaction_cannot_be_located()
    {
        var processId = "Not existing";
        var command = new SetConsumerHasMovedIn(processId);

        await Assert.ThrowsAsync<TransactionNotFoundException>(() => InvokeCommandAsync(command)).ConfigureAwait(false);
    }

    [Fact]
    public async Task Business_process_is_marked_as_completed()
    {
        await ConsumerHasMovedIn().ConfigureAwait(false);

        AssertQueuedCommand.QueuedCommand<NotifyCurrentEnergySupplier>(GetService<IDbConnectionFactory>());
        AssertTransaction()
            .BusinessProcessCompleted();
    }

    [Fact]
    public async Task Notification_of_grid_operator_is_scheduled()
    {
        await ConsumerHasMovedIn().ConfigureAwait(false);

        AssertQueuedCommand.QueuedCommand<NotifyGridOperator>(GetService<IDbConnectionFactory>());
    }

    [Fact]
    public async Task Notification_of_current_energy_supplier_is_scheduled()
    {
        await ConsumerHasMovedIn().ConfigureAwait(false);

        AssertQueuedCommand.QueuedCommand<NotifyCurrentEnergySupplier>(GetService<IDbConnectionFactory>());
    }

    private async Task<MoveInTransaction> ConsumerHasMovedIn()
    {
        var transaction = await StartMoveInTransaction();
        await InvokeCommandAsync(new SetConsumerHasMovedIn(transaction.ProcessId!)).ConfigureAwait(false);
        return transaction;
    }

    private async Task<MoveInTransaction> StartMoveInTransaction()
    {
        var transaction = new MoveInTransaction(
            SampleData.TransactionId,
            SampleData.MeteringPointNumber,
            SampleData.SupplyStart,
            SampleData.CurrentEnergySupplierNumber,
            SampleData.OriginalMessageId,
            SampleData.NewEnergySupplierNumber,
            SampleData.ConsumerId,
            SampleData.ConsumerName,
            SampleData.ConsumerIdType);

        transaction.AcceptedByBusinessProcess(BusinessRequestResult.Succeeded(Guid.NewGuid().ToString()).ProcessId!, SampleData.MeteringPointNumber);
        transaction.MarkMeteringPointMasterDataAsSent();
        _transactionRepository.Add(transaction);
        await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
        return transaction;
    }

    private AssertTransaction AssertTransaction()
    {
        return MoveIn.AssertTransaction
            .Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>());
    }
}
