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
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Messaging.Application.Actors;
using Messaging.Application.Configuration.Authentication;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.RequestChangeOfSupplier;
using Messaging.Domain.OutgoingMessages;
using Messaging.IntegrationTests.CimMessageAdapter.Stubs;
using Messaging.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Messaging.IntegrationTests.CimMessageAdapter.Messages.RequestChangeOfSupplier
{
    [IntegrationTest]
    public class RequestChangeOfSupplierReceiverTests : TestBase, IAsyncLifetime
    {
        private const string SenderId = "1234567890123";
        private readonly MessageParser _messageParser;
        private readonly IMarketActorAuthenticator _marketActorAuthenticator;
        private readonly ITransactionIds _transactionIds;
        private readonly IMessageIds _messageIds;
        private MessageQueueDispatcherStub<Messaging.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction> _messageQueueDispatcherSpy = new();
        private List<Claim> _claims = new();

        public RequestChangeOfSupplierReceiverTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _messageParser = GetService<MessageParser>();
            _transactionIds = GetService<ITransactionIds>();
            _messageIds = GetService<IMessageIds>();
            _marketActorAuthenticator = GetService<IMarketActorAuthenticator>();
        }

        public async Task InitializeAsync()
        {
            var createActorCommand =
                new CreateActor(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), SenderId);
            await InvokeCommandAsync(createActorCommand).ConfigureAwait(false);
            _claims = new List<Claim>()
            {
                new("azp", createActorCommand.B2CId),
                new("actorid", createActorCommand.IdentificationNumber),
                new("actoridtype", "GLN"),
                new(ClaimTypes.Role, "electricalsupplier"),
            };

            await _marketActorAuthenticator.AuthenticateAsync(CreateIdentity());
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Receiver_id_must_be_known()
        {
            var unknownReceiverId = "5790001330550";
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithReceiverId(unknownReceiverId)
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            AssertContainsError(result, "B2B-008");
        }

        [Fact]
        public async Task Receiver_role_must_be_metering_point_administrator()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithReceiverRole("DDK")
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            AssertContainsError(result, "B2B-008");
        }

        [Fact]
        public async Task Sender_role_type_must_be_the_role_of_energy_supplier()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithSenderRole("DDK")
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            AssertContainsError(result, "B2B-008");
        }

        [Fact]
        public async Task Authenticated_user_must_hold_the_role_type_as_specified_in_message()
        {
            await _marketActorAuthenticator.AuthenticateAsync(CreateIdentityWithoutRoles());
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            AssertContainsError(result, "B2B-008");
        }

        [Fact]
        public async Task Sender_id_must_match_the_organization_of_the_current_authenticated_user()
        {
            await _marketActorAuthenticator.AuthenticateAsync(CreateIdentity("1234567890123"));
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            AssertContainsError(result, "B2B-008");
        }

        [Fact]
        public async Task Return_failure_if_xml_schema_for_business_process_type_does_not_exist()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier("CimMessageAdapter//Messages//Xml//BadRequestChangeOfSupplier.xml")
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            Assert.False(result.Success);
            AssertContainsError(result, "B2B-001");
        }

        [Fact]
        public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithSenderId(SenderId)
                .Message();

            await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            var transaction = _messageQueueDispatcherSpy.CommittedItems.FirstOrDefault();
            Assert.NotNull(transaction);
        }

        [Fact]
        public async Task Activity_records_are_not_committed_to_queue_if_any_message_header_values_are_invalid()
        {
            await SimulateDuplicationOfMessageIds(_messageIds).ConfigureAwait(false);

            Assert.Empty(_messageQueueDispatcherSpy.CommittedItems);
        }

        [Fact]
        public async Task Activity_records_must_have_unique_transaction_ids()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithSenderId(SenderId)
                .DuplicateMarketActivityRecords()
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            AssertContainsError(result, "B2B-005");
            Assert.Empty(_messageQueueDispatcherSpy.CommittedItems);
        }

        private static ClaimsPrincipal CreateClaimsPrincipal(IEnumerable<Claim> claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        private static void AssertContainsError(Result result, string errorCode)
        {
            Assert.Contains(result.Errors, error => error.Code.Equals(errorCode, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<Result> ReceiveRequestChangeOfSupplierMessage(Stream message)
        {
            return await CreateMessageReceiver()
                .ReceiveAsync(await ParseMessageAsync(message).ConfigureAwait(false));
        }

        private MessageReceiver<Messaging.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction> CreateMessageReceiver()
        {
            _messageQueueDispatcherSpy = new MessageQueueDispatcherStub<Messaging.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction>();
            var messageReceiver = new RequestChangeOfSupplierReceiver(
                _messageIds,
                _messageQueueDispatcherSpy,
                _transactionIds,
                new SenderAuthorizer(_marketActorAuthenticator));
            return messageReceiver;
        }

        private MessageReceiver<Messaging.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction> CreateMessageReceiver(IMessageIds messageIds)
        {
            _messageQueueDispatcherSpy = new MessageQueueDispatcherStub<Messaging.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction>();
            var messageReceiver = new RequestChangeOfSupplierReceiver(messageIds, _messageQueueDispatcherSpy, _transactionIds, new SenderAuthorizer(_marketActorAuthenticator));
            return messageReceiver;
        }

        private async Task SimulateDuplicationOfMessageIds(IMessageIds messageIds)
        {
            var messageBuilder = BusinessMessageBuilder.RequestChangeOfSupplier();

            using var originalMessage = messageBuilder.Message();
            await CreateMessageReceiver(messageIds).ReceiveAsync(await ParseMessageAsync(originalMessage).ConfigureAwait(false))
                .ConfigureAwait(false);

            using var duplicateMessage = messageBuilder.Message();
            await CreateMessageReceiver(messageIds).ReceiveAsync(await ParseMessageAsync(duplicateMessage).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        private Task<MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>> ParseMessageAsync(Stream message)
        {
            return _messageParser.ParseAsync(message, CimFormat.Xml);
        }

        private ClaimsPrincipal CreateIdentity()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(_claims));
        }

        private ClaimsPrincipal CreateIdentity(string actorIdentifier)
        {
            var claims = _claims.ToList();
            claims.Remove(claims.Find(claim => claim.Type.Equals("actorid", StringComparison.OrdinalIgnoreCase))!);
            claims.Add(new Claim("actorid", actorIdentifier));
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        private ClaimsPrincipal CreateIdentityWithoutRoles()
        {
            var claims = _claims.ToList();
            claims.RemoveAll(claim => claim.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase));
            return CreateClaimsPrincipal(claims);
        }
    }
}
