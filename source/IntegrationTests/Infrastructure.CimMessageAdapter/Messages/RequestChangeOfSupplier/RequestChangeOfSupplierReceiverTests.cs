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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestChangeOfSupplier;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestChangeOfSupplier;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.ValidationErrors;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Stubs;
using Xunit;
using Xunit.Categories;
using Result = Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Result;
using SenderAuthorizer = Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestChangeOfSupplier.SenderAuthorizer;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages.RequestChangeOfSupplier
{
    [IntegrationTest]
    public class RequestChangeOfSupplierReceiverTests : TestBase, IAsyncLifetime
    {
        private readonly MessageParser _messageParser;
        private readonly IMarketActorAuthenticator _marketActorAuthenticator;
        private readonly ITransactionIds _transactionIds;
        private readonly IMessageIdRepository _messageIdRepository;
        private readonly DefaultProcessTypeValidator _processTypeValidator;
        private readonly DefaultMessageTypeValidator _messageTypeValidator;
        private readonly MasterDataReceiverResponsibleVerification _masterDataReceiverResponsibleVerification;
        private MessageQueueDispatcherStub<global::Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction> _messageQueueDispatcherSpy = new();
        private List<Claim> _claims = new();

        public RequestChangeOfSupplierReceiverTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _messageParser = GetService<MessageParser>();
            _transactionIds = GetService<ITransactionIds>();
            _messageIdRepository = GetService<IMessageIdRepository>();
            _marketActorAuthenticator = GetService<IMarketActorAuthenticator>();
            _processTypeValidator = GetService<DefaultProcessTypeValidator>();
            _messageTypeValidator = GetService<DefaultMessageTypeValidator>();
            _masterDataReceiverResponsibleVerification = GetService<MasterDataReceiverResponsibleVerification>();
        }

        public async Task InitializeAsync()
        {
            await InvokeCommandAsync(new CreateActorCommand(Guid.NewGuid().ToString(), SampleData.StsAssignedUserId, SampleData.ActorNumber)).ConfigureAwait(false);
            _claims = new List<Claim>()
            {
                new(ClaimsMap.UserId, new CreateActorCommand(Guid.NewGuid().ToString(), SampleData.StsAssignedUserId, SampleData.ActorNumber).B2CId),
                ClaimsMap.RoleFrom(MarketRole.EnergySupplier),
            };

#pragma warning disable CA2007
            await _marketActorAuthenticator.AuthenticateAsync(CreateIdentity(), CancellationToken.None);
#pragma warning restore CA2007
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
#pragma warning disable CA2007

        [Fact]
        public async Task Receiver_id_must_be_known()
        {
            var unknownReceiverId = "5790001330550";
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithReceiverId(unknownReceiverId)
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            Assert.Contains(result.Errors, error => error is InvalidReceiverId);
        }

        [Fact]
        public async Task Receiver_role_must_be_metering_point_administrator()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithReceiverRole("DDK")
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            Assert.Contains(result.Errors, error => error is InvalidReceiverRole);
        }

        [Fact]
        public async Task Sender_role_type_must_be_the_role_of_energy_supplier()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithSenderRole("DDK")
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            Assert.Contains(result.Errors, error => error is AuthenticatedUserDoesNotHoldRequiredRoleType);
        }

        [Fact]
        public async Task Authenticated_user_must_hold_the_role_type_as_specified_in_message()
        {
            await _marketActorAuthenticator.AuthenticateAsync(CreateIdentityWithoutRoles(), CancellationToken.None);
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            Assert.Contains(result.Errors, error => error is AuthenticatedUserDoesNotHoldRequiredRoleType);
        }

        [Fact]
        public async Task Sender_id_must_match_the_organization_of_the_current_authenticated_user()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            Assert.Contains(result.Errors, error => error is AuthenticatedUserDoesNotMatchSenderId);
        }

        [Fact]
        public async Task Return_failure_if_xml_schema_for_business_reason_does_not_exist()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier("Infrastructure.CimMessageAdapter//Messages//Xml//BadRequestChangeOfSupplier.xml")
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, error => error is InvalidBusinessReasonOrVersion);
        }

        [Fact]
        public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithSenderId(SampleData.ActorNumber)
                .Message();

            await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            var transaction = _messageQueueDispatcherSpy.CommittedItems.FirstOrDefault();
            Assert.NotNull(transaction);
        }

        [Fact]
        public async Task Activity_records_are_not_committed_to_queue_if_any_message_header_values_are_invalid()
        {
            await SimulateDuplicationOfMessageIds(_messageIdRepository).ConfigureAwait(false);

            Assert.Empty(_messageQueueDispatcherSpy.CommittedItems);
        }

        [Fact]
        public async Task Activity_records_must_have_unique_transaction_ids()
        {
            await using var message = BusinessMessageBuilder
                .RequestChangeOfSupplier()
                .WithSenderId(SampleData.ActorNumber)
                .DuplicateMarketActivityRecords()
                .Message();

            var result = await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            Assert.Contains(result.Errors, error => error is DuplicateTransactionIdDetected);
            Assert.Empty(_messageQueueDispatcherSpy.CommittedItems);
        }

        private static ClaimsPrincipal CreateClaimsPrincipal(IEnumerable<Claim> claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        private async Task<Result> ReceiveRequestChangeOfSupplierMessage(Stream message)
        {
            return await CreateMessageReceiver()
                .ReceiveAsync(await ParseMessageAsync(message).ConfigureAwait(false), CancellationToken.None);
        }

        private MessageReceiver<global::Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction> CreateMessageReceiver()
        {
            _messageQueueDispatcherSpy = new MessageQueueDispatcherStub<global::Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction>();
            var messageReceiver = new RequestChangeOfSupplierReceiver(
                _messageIdRepository,
                _messageQueueDispatcherSpy,
                _transactionIds,
                new SenderAuthorizer(_marketActorAuthenticator),
                _processTypeValidator,
                _messageTypeValidator,
                _masterDataReceiverResponsibleVerification);
            return messageReceiver;
        }

        private MessageReceiver<global::Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction> CreateMessageReceiver(IMessageIdRepository messageIdRepository)
        {
            _messageQueueDispatcherSpy = new MessageQueueDispatcherStub<global::Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Queues.RequestChangeOfSupplierTransaction>();
            var messageReceiver = new RequestChangeOfSupplierReceiver(messageIdRepository, _messageQueueDispatcherSpy, _transactionIds, new SenderAuthorizer(_marketActorAuthenticator), _processTypeValidator, _messageTypeValidator, _masterDataReceiverResponsibleVerification);
            return messageReceiver;
        }

        private async Task SimulateDuplicationOfMessageIds(IMessageIdRepository messageIdRepository)
        {
            var messageBuilder = BusinessMessageBuilder.RequestChangeOfSupplier();

            using var originalMessage = messageBuilder.Message();
            await CreateMessageReceiver(messageIdRepository).ReceiveAsync(await ParseMessageAsync(originalMessage).ConfigureAwait(false), CancellationToken.None)
                .ConfigureAwait(false);

            using var duplicateMessage = messageBuilder.Message();
            await CreateMessageReceiver(messageIdRepository).ReceiveAsync(await ParseMessageAsync(duplicateMessage).ConfigureAwait(false), CancellationToken.None)
                .ConfigureAwait(false);
        }

        private Task<MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand>> ParseMessageAsync(Stream message)
        {
            return _messageParser.ParseAsync(message, DocumentFormat.Xml, CancellationToken.None);
        }

        private ClaimsPrincipal CreateIdentity()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(_claims));
        }

        private ClaimsPrincipal CreateIdentityWithoutRoles()
        {
            var claims = _claims.ToList();
            claims.RemoveAll(claim => claim.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase));
            return CreateClaimsPrincipal(claims);
        }
    }
}
