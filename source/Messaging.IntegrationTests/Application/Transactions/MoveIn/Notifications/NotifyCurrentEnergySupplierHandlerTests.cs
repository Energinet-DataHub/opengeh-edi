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

        AssertTransaction()
            .HasEndOfSupplyNotificationState(MoveInTransaction.EndOfSupplyNotificationState.EnergySupplierWasNotified);
        AssertMessage(DocumentType.GenericNotification, BusinessReasonCode.CustomerMoveInOrMoveOut.Code)
            .HasReceiverId(SampleData.CurrentEnergySupplierNumber)
            .HasReceiverRole(MarketRoles.EnergySupplier)
            .HasSenderId(DataHubDetails.IdentificationNumber)
            .HasSenderRole(MarketRoles.MeteringPointAdministrator)
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

    private AssertOutgoingMessage AssertMessage(DocumentType documentType, string processType)
    {
        return AssertOutgoingMessage.OutgoingMessage(SampleData.TransactionId, documentType.Name, processType, GetService<IDbConnectionFactory>());
    }

    private AssertTransaction AssertTransaction()
    {
        return MoveIn.AssertTransaction
            .Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>());
    }
}
