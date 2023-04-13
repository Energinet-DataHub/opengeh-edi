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
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Application.Transactions.MoveIn;
using Dapper;
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Domain.Transactions.MoveIn;
using Infrastructure.Configuration.InternalCommands;
using Infrastructure.Transactions;
using IntegrationTests.Application.IncomingMessages;
using IntegrationTests.Assertions;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Application.Transactions.MoveIn
{
    [IntegrationTest]
    public class RequestMoveInTests : TestBase
    {
        public RequestMoveInTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
        }

        [Fact]
        public async Task Transaction_is_started()
        {
            var incomingMessage = MessageBuilder()
                .WithSenderId(SampleData.SenderId)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);

            var assertTransaction = await AssertTransaction.TransactionAsync(SampleData.ActorProvidedId, GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
            assertTransaction.HasState(MoveInTransaction.State.Started)
                .HasStartedByMessageId(incomingMessage.Message.MessageId)
                .HasNewEnergySupplierId(incomingMessage.Message.SenderId)
                .HasConsumerId(incomingMessage.MarketActivityRecord.ConsumerId!)
                .HasConsumerName(incomingMessage.MarketActivityRecord.ConsumerName!)
                .HasConsumerIdType(incomingMessage.MarketActivityRecord.ConsumerIdType!)
                .HasEndOfSupplyNotificationState(MoveInTransaction.NotificationState.NotNeeded)
                .HasGridOperatorNotificationState(MoveInTransaction.NotificationState.Pending)
                .HasCustomerMasterDataSentToGridOperatorState(MoveInTransaction.MasterDataState.Pending)
                .HasRequestedByActorNumber(SampleData.SenderId);
        }

        [Fact]
        public async Task A_confirm_message_created_when_the_transaction_is_accepted()
        {
            await GivenRequestHasBeenAccepted().ConfigureAwait(false);

            await ConfirmMessageIsCreated().ConfigureAwait(false);
        }

        [Fact]
        public async Task Fetch_metering_point_master_data_when_the_transaction_is_accepted()
        {
            await GivenRequestHasBeenAccepted().ConfigureAwait(false);

            AssertCommand<FetchMeteringPointMasterData>();
        }

        [Fact]
        public async Task Customer_master_data_is_retrieved_when_the_transaction_is_accepted()
        {
            await GivenRequestHasBeenAccepted().ConfigureAwait(false);

            AssertCommand<FetchCustomerMasterData>();
        }

        [Fact]
        public async Task A_reject_message_is_created_when_the_transaction_is_rejected()
        {
            var httpClientMock = GetHttpClientMock();
            httpClientMock.RespondWithValidationErrors(new List<string> { "InvalidConsumer" });

            var incomingMessage = MessageBuilder()
                .WithProcessType(ProcessType.MoveIn)
                .WithReceiver(SampleData.ReceiverId)
                .WithSenderId(SampleData.SenderId)
                .WithConsumerName(null)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);

            await RejectMessageIsCreated().ConfigureAwait(false);
        }

        [Fact]
        public async Task A_reject_message_is_created_when_the_sender_id_does_not_match_energy_supplier_id()
        {
            var incomingMessage = MessageBuilder()
                .WithProcessType(ProcessType.MoveIn)
                .WithReceiver(SampleData.ReceiverId)
                .WithSenderId("1234567890123")
                .WithConsumerName(null)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);

            var rejectMessage = await RejectMessage().ConfigureAwait(false);
            rejectMessage.HasReceiverId(incomingMessage.Message.SenderId);
        }

        [Fact]
        public async Task A_reject_message_is_created_when_the_energy_supplier_id_is_empty()
        {
            var incomingMessage = MessageBuilder()
                .WithProcessType(ProcessType.MoveIn)
                .WithReceiver(SampleData.ReceiverId)
                .WithSenderId(SampleData.SenderId)
                .WithEnergySupplierId(null)
                .WithConsumerName(null)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);

            await RejectMessageIsCreated().ConfigureAwait(false);
        }

        private static IncomingMessageBuilder MessageBuilder()
        {
            return new IncomingMessageBuilder()
                .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
                .WithMessageId(SampleData.OriginalMessageId)
                .WithMarketEvaluationPointId(SampleData.MeteringPointNumber)
                .WithTransactionId(SampleData.ActorProvidedId.Id);
        }

        private async Task GivenRequestHasBeenAccepted()
        {
            var incomingMessage = MessageBuilder()
                .WithProcessType(ProcessType.MoveIn)
                .WithReceiver(SampleData.ReceiverId)
                .WithSenderId(SampleData.SenderId)
                .WithConsumerName(SampleData.ConsumerName)
                .WithMarketEvaluationPointId(SampleData.MeteringPointNumber)
                .Build();

            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        }

        private HttpClientSpy GetHttpClientMock()
        {
            var adapter = GetService<IHttpClientAdapter>();
            return adapter as HttpClientSpy ?? throw new InvalidCastException();
        }

        private AssertQueuedCommand AssertCommand<TCommand>()
        {
            return AssertQueuedCommand.QueuedCommand<TCommand>(
                GetService<IDatabaseConnectionFactory>(),
                GetService<InternalCommandMapper>());
        }

        private async Task ConfirmMessageIsCreated()
        {
            var currentTransactionId = await GetTransactionIdAsync().ConfigureAwait(false);
            var assertMessage = await AssertOutgoingMessage.OutgoingMessageAsync(
                    currentTransactionId,
                    MessageType.ConfirmRequestChangeOfSupplier.Name,
                    ProcessType.MoveIn.Name,
                    MarketRole.EnergySupplier,
                    GetService<IDatabaseConnectionFactory>())
                .ConfigureAwait(false);

            assertMessage.HasReceiverId(SampleData.NewEnergySupplierNumber);
            assertMessage.HasReceiverRole(MarketRole.EnergySupplier.Name);
            assertMessage.HasMessageRecordValue<MarketActivityRecord>(
                record => record.OriginalTransactionId,
                SampleData.ActorProvidedId.Id);
            assertMessage.HasMessageRecordValue<MarketActivityRecord>(
                record => record.MarketEvaluationPointId,
                SampleData.MeteringPointNumber);
        }

        private async Task RejectMessageIsCreated()
        {
            var sut = await RejectMessage().ConfigureAwait(false);
            sut.HasReceiverId(SampleData.NewEnergySupplierNumber);
        }

        private async Task<AssertOutgoingMessage> RejectMessage()
        {
            var assertMessage = await AssertOutgoingMessage
                .OutgoingMessageAsync(
                    MessageType.RejectRequestChangeOfSupplier.Name,
                    ProcessType.MoveIn.Name,
                    MarketRole.EnergySupplier,
                    GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
            assertMessage.HasReceiverRole(MarketRole.EnergySupplier.Name);
            assertMessage
                .HasMessageRecordValue<Domain.OutgoingMessages.RejectRequestChangeOfSupplier.MarketActivityRecord>(
                    record => record.MarketEvaluationPointId, SampleData.MeteringPointNumber);
            assertMessage
                .HasMessageRecordValue<Domain.OutgoingMessages.RejectRequestChangeOfSupplier.MarketActivityRecord>(
                    record => record.OriginalTransactionId, SampleData.ActorProvidedId.Id);

            return assertMessage;
        }

        private async Task<Guid> GetTransactionIdAsync()
        {
            using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
            return await connection
                .QueryFirstAsync<Guid>(
                    "SELECT TOP(1) TransactionId FROM dbo.MoveInTransactions WHERE ActorProvidedId = @ActorProvidedId",
                    new { ActorProvidedId = SampleData.ActorProvidedId.Id }).ConfigureAwait(false);
        }
    }
}
