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
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class CreateEndOfSupplyNotificationTests : TestBase
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMoveInTransactionRepository _transactionRepository;

    public CreateEndOfSupplyNotificationTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
        _transactionRepository = GetService<IMoveInTransactionRepository>();
    }

    [Fact]
    public async Task An_exception_is_thrown_if_transaction_cannot_be_located()
    {
        var transactionId = "Not existing";
        var command = CreateCommand(transactionId);

        await Assert.ThrowsAsync<TransactionNotFoundException>(() => InvokeCommandAsync(command)).ConfigureAwait(false);
    }

    [Fact]
    public async Task The_current_energy_supplier_is_notified_about_end_of_supply()
    {
        var transaction = await ConsumerHasMovedIn().ConfigureAwait(false);

        await InvokeCommandAsync(CreateCommand(transaction.TransactionId)).ConfigureAwait(false);

        AssertTransaction()
            .HasEndOfSupplyNotificationState(MoveInTransaction.EndOfSupplyNotificationState.EnergySupplierWasNotified);
        AssertMessage(transaction.TransactionId, DocumentType.GenericNotification.ToString(), BusinessReasonCode.CustomerMoveInOrMoveOut.Code)
            .HasReceiverId(transaction.CurrentEnergySupplierId!)
            .HasReceiverRole(MarketRoles.EnergySupplier)
            .HasSenderId(DataHubDetails.IdentificationNumber)
            .HasSenderRole(MarketRoles.MeteringPointAdministrator)
            .HasReasonCode(null)
            .WithMarketActivityRecord()
                .HasId()
                .HasValidityStart(transaction.EffectiveDate.ToDateTimeUtc())
                .HasOriginalTransactionId(transaction.TransactionId)
                .HasMarketEvaluationPointId(transaction.MarketEvaluationPointId);
    }

    private CreateEndOfSupplyNotification CreateCommand(string transactionId)
    {
        return new CreateEndOfSupplyNotification(transactionId, _systemDateTimeProvider.Now(), SampleData.MeteringPointNumber, SampleData.CurrentEnergySupplierNumber);
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
            _systemDateTimeProvider.Now(),
            SampleData.CurrentEnergySupplierNumber,
            SampleData.OriginalMessageId,
            SampleData.NewEnergySupplierNumber,
            SampleData.ConsumerId,
            SampleData.ConsumerName,
            SampleData.ConsumerIdType);

        transaction.AcceptedByBusinessProcess(BusinessRequestResult.Succeeded(Guid.NewGuid().ToString()).ProcessId!, SampleData.MeteringPointNumber);
        _transactionRepository.Add(transaction);
        await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
        return transaction;
    }

    private AssertOutgoingMessage AssertMessage(string transactionId, string documentType, string processType)
    {
        return AssertOutgoingMessage.OutgoingMessage(transactionId, documentType, processType, GetService<IDbConnectionFactory>());
    }

    private AssertTransaction AssertTransaction()
    {
        return MoveIn.AssertTransaction
            .Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>());
    }
}
