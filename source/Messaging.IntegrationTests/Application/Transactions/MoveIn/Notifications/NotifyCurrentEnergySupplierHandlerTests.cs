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
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.Transactions.MoveIn.Notifications;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn.Notifications;

public class NotifyCurrentEnergySupplierHandlerTests
    : TestBase, IAsyncLifetime
{
    public NotifyCurrentEnergySupplierHandlerTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    public Task InitializeAsync()
    {
        return Scenario.Details(
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
                GetService<B2BContext>())
            .IsEffective()
            .WithGridOperatorForMeteringPoint(
                SampleData.IdOfGridOperatorForMeteringPoint,
                SampleData.NumberOfGridOperatorForMeteringPoint)
            .BuildAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task The_current_energy_supplier_is_notified_about_end_of_supply()
    {
        await InvokeCommandAsync(CreateCommand()).ConfigureAwait(false);

        var assertTransaction = await AssertTransactionAsync().ConfigureAwait(false);
        assertTransaction.HasEndOfSupplyNotificationState(MoveInTransaction.NotificationState.WasNotified);
        var assertOutgoingMessage = await AssertMessageAsync(MessageType.GenericNotification, BusinessReasonCode.CustomerMoveInOrMoveOut.Code).ConfigureAwait(false);
        assertOutgoingMessage.HasReceiverId(SampleData.CurrentEnergySupplierNumber)
            .HasReceiverRole(MarketRole.EnergySupplier.ToString())
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasSenderRole(MarketRole.MeteringPointAdministrator.ToString())
            .WithMarketActivityRecord()
            .HasId()
            .HasValidityStart(SampleData.SupplyStart)
            .HasOriginalTransactionId(SampleData.TransactionId)
            .HasMarketEvaluationPointId(SampleData.MeteringPointNumber);
    }

    private static NotifyCurrentEnergySupplier CreateCommand()
    {
        return new NotifyCurrentEnergySupplier(
            SampleData.TransactionId,
            SampleData.SupplyStart,
            SampleData.MeteringPointNumber,
            SampleData.CurrentEnergySupplierNumber);
    }

    private async Task<AssertOutgoingMessage> AssertMessageAsync(MessageType messageType, string processType)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(SampleData.TransactionId, messageType.Name, processType, GetService<IEdiDatabaseConnection>()).ConfigureAwait(false);
    }

    private async Task<AssertTransaction> AssertTransactionAsync()
    {
        return await MoveIn.AssertTransaction
            .TransactionAsync(SampleData.TransactionId, GetService<IEdiDatabaseConnection>()).ConfigureAwait(false);
    }
}
