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
using Messaging.Application.Actors;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Application.Transactions.MoveIn.Notifications;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn.Notifications;

public class NotifyGridOperatorHandlerTests
    : TestBase
{
    private readonly IMoveInTransactionRepository _transactionRepository;

    public NotifyGridOperatorHandlerTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _transactionRepository = GetService<IMoveInTransactionRepository>();
    }

    [Fact]
    public async Task Grid_operator_is_notified_about_the_move_in()
    {
        await ConsumerHasMovedIn().ConfigureAwait(false);

        var command = new NotifyGridOperator(SampleData.TransactionId);
        await InvokeCommandAsync(command).ConfigureAwait(false);

        AssertOutgoingMessage.OutgoingMessage(
                SampleData.TransactionId,
                DocumentType.GenericNotification.Name,
                ProcessType.MoveIn.Code,
                GetService<IDbConnectionFactory>())
            .HasSenderId(DataHubDetails.IdentificationNumber)
            .HasSenderRole(MarketRoles.MeteringPointAdministrator)
            .HasReceiverRole(MarketRoles.GridOperator)
            .HasReceiverId(SampleData.NumberOfGridOperatorForMeteringPoint)
            .WithMarketActivityRecord()
            .HasValidityStart(SampleData.SupplyStart)
            .HasId()
            .HasOriginalTransactionId(SampleData.TransactionId)
            .HasMarketEvaluationPointId(SampleData.MeteringPointNumber);
    }

    private async Task<MoveInTransaction> ConsumerHasMovedIn()
    {
        var transaction = await StartMoveInTransaction();
        await InvokeCommandAsync(new SetConsumerHasMovedIn(transaction.ProcessId!)).ConfigureAwait(false);
        return transaction;
    }

    private async Task<MoveInTransaction> StartMoveInTransaction()
    {
        await SetupGridOperatorDetailsAsync();
        await SetupMasterDataDetailsAsync();
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

    private Task SetupGridOperatorDetailsAsync()
    {
        return InvokeCommandAsync(new CreateActor(
            SampleData.IdOfGridOperatorForMeteringPoint.ToString(),
            SampleData.NumberOfGridOperatorForMeteringPoint));
    }

    private Task SetupMasterDataDetailsAsync()
    {
        var marketEvaluationPoint = MarketEvaluationPoint.Create(
            SampleData.CurrentEnergySupplierNumber,
            SampleData.MeteringPointNumber);
        marketEvaluationPoint.SetGridOperatorId(SampleData.IdOfGridOperatorForMeteringPoint);

        GetService<IMarketEvaluationPointRepository>()
            .Add(marketEvaluationPoint);
        return Task.CompletedTask;
    }
}
