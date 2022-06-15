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
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Processing.Domain.SeedWork;
using Xunit;
using MarketEvaluationPoint = Messaging.Domain.MasterData.MarketEvaluationPoints.MarketEvaluationPoint;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class CompleteMoveInTests : TestBase
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMoveInTransactionRepository _transactionRepository;

    public CompleteMoveInTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
        _transactionRepository = GetService<IMoveInTransactionRepository>();
    }

    [Fact]
    public async Task Transaction_is_completed()
    {
        await CompleteMoveIn().ConfigureAwait(false);

        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>())
            .WithState(MoveInTransaction.State.Completed);
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
        var transaction = await CompleteMoveIn().ConfigureAwait(false);

        AssertThat(transaction.TransactionId, DocumentType.GenericNotification.ToString(), BusinessReasonCode.CustomerMoveInOrMoveOut.Code)
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

    private async Task<MoveInTransaction> CompleteMoveIn()
    {
        var transaction = await StartMoveInTransaction();
        await InvokeCommandAsync(new CompleteMoveInTransaction(transaction.ProcessId!)).ConfigureAwait(false);
        return transaction;
    }

    private async Task<MoveInTransaction> StartMoveInTransaction()
    {
        await SetupMasterDataDetailsAsync();
        var transaction = new MoveInTransaction(
            SampleData.TransactionId,
            SampleData.MateringPointNumber,
            _systemDateTimeProvider.Now(),
            SampleData.EnergySupplierNumber);

        transaction.Start(BusinessRequestResult.Succeeded(Guid.NewGuid().ToString()));
        _transactionRepository.Add(transaction);
        await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
        return transaction;
    }

    private Task SetupMasterDataDetailsAsync()
    {
        GetService<IMarketEvaluationPointRepository>().Add(MarketEvaluationPoint.Create(SampleData.EnergySupplierNumber, SampleData.MateringPointNumber));
        return Task.CompletedTask;
    }

    private AssertOutgoingMessage AssertThat(string transactionId, string documentType, string processType)
    {
        return AssertOutgoingMessage.OutgoingMessage(transactionId, documentType, processType, GetService<IDbConnectionFactory>());
    }
}
