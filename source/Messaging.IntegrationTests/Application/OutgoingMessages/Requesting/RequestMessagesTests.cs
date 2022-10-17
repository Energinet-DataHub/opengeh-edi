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
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.IncomingMessages;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Requesting;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.Infrastructure.OutgoingMessages.Common.Xml;
using Messaging.Infrastructure.OutgoingMessages.Requesting;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;

namespace Messaging.IntegrationTests.Application.OutgoingMessages.Requesting
{
    public class RequestMessagesTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly MessageStorageSpy _messageStorage;

        public RequestMessagesTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _messageStorage = (MessageStorageSpy)GetService<IMessageStorage>();
        }

        [Fact]
        public async Task Message_is_dispatched_on_request()
        {
            var incomingMessage1 = await MessageArrived().ConfigureAwait(false);
            var incomingMessage2 = await MessageArrived().ConfigureAwait(false);
            var outgoingMessage1 = _outgoingMessageStore.GetByTransactionId(incomingMessage1.Message.MessageId)!;
            var outgoingMessage2 = _outgoingMessageStore.GetByTransactionId(incomingMessage2.Message.MessageId)!;

            var requestedMessageIds = new List<string> { outgoingMessage1.Id.ToString(), outgoingMessage2.Id.ToString(), };
            var request = CreateRequest(requestedMessageIds);
            await RequestMessages(request).ConfigureAwait(false);

            _messageStorage.MessageHasBeenSavedInStorage();
            var command = AssertCommand<SendSuccessNotification>().Command() as SendSuccessNotification;
            Assert.NotNull(command);
            Assert.Equal(request.ClientProvidedDetails.RequestId, command?.RequestId);
            Assert.Equal(request.ClientProvidedDetails.IdempotencyId, command?.IdempotencyId);
            Assert.Equal(request.ClientProvidedDetails.ReferenceId, command?.ReferenceId);
            Assert.Equal(request.ClientProvidedDetails.DocumentType, command?.DocumentType);
            Assert.Equal(CimFormat.Xml.Name, command?.RequestedFormat);
            Assert.NotNull(command?.MessageStorageLocation);
        }

        [Fact]
        public async Task Requested_message_ids_must_exist()
        {
            var nonExistingMessage = new List<string> { Guid.NewGuid().ToString() };
            var request = CreateRequest(nonExistingMessage);

            await RequestMessages(request).ConfigureAwait(false);

            Assert.Null(_messageStorage.SavedMessage);
            AssertErrorResponse(request, "DatasetNotFound");
        }

        [Fact]
        public async Task Respond_with_error_if_requested_message_format_can_not_be_handled()
        {
            GivenTheRequestedDocumentFormatIsNotSupported();

            var incomingMessage = await MessageArrived().ConfigureAwait(false);
            var outgoingMessage = _outgoingMessageStore.GetByTransactionId(incomingMessage.Message.MessageId)!;

            var requestedMessageIds = new List<string> { outgoingMessage.Id.ToString(), };
            var request = CreateRequest(requestedMessageIds);
            await RequestMessages(request).ConfigureAwait(false);

            Assert.Null(_messageStorage.SavedMessage);
            AssertErrorResponse(request, "InternalError");
        }

        [Fact]
        public async Task Respond_with_error_if_requested_document_type_is_unknown()
        {
            var incomingMessage = await MessageArrived().ConfigureAwait(false);
            var outgoingMessage = _outgoingMessageStore.GetByTransactionId(incomingMessage.Message.MessageId)!;

            var requestedMessageIds = new List<string> { outgoingMessage.Id.ToString(), };
            var request = CreateRequest(requestedMessageIds, "UnknownDocumentType");
            await RequestMessages(request).ConfigureAwait(false);

            Assert.Null(_messageStorage.SavedMessage);
            AssertErrorResponse(request, "InternalError");
        }

        private static RequestMessages CreateRequest(List<string> requestedMessageIds, string documentType = "RejectRequestChangeOfSupplier")
        {
            var clientProvidedDetails = new ClientProvidedDetails(
                Guid.NewGuid(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                documentType,
                CimFormat.Xml.Name);

            return new RequestMessages(
                requestedMessageIds,
                clientProvidedDetails);
        }

        private static IncomingMessageBuilder MessageBuilder()
        {
            return new IncomingMessageBuilder()
                .WithProcessType(ProcessType.MoveIn.Code);
        }

        private async Task<RequestChangeOfSupplierTransaction> MessageArrived()
        {
            var incomingMessage = MessageBuilder()
                .WithSenderId(SampleData.SenderId)
                .Build();
            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
            return incomingMessage;
        }

        private Task RequestMessages(RequestMessages request)
        {
            return GetService<IMediator>().Send(request);
        }

        private void GivenTheRequestedDocumentFormatIsNotSupported()
        {
            RegisterInstance(new DocumentFactory(new List<DocumentWriter>()));
        }

        private void AssertErrorResponse(RequestMessages request, string reason)
        {
            var command = AssertCommand<SendFailureNotification>()
                .Command() as SendFailureNotification;
            Assert.NotNull(command);
            Assert.Equal(request.ClientProvidedDetails.RequestId, command?.RequestId);
            Assert.Equal(request.ClientProvidedDetails.IdempotencyId, command?.IdempotencyId);
            Assert.Equal(request.ClientProvidedDetails.ReferenceId, command?.ReferenceId);
            Assert.Equal(request.ClientProvidedDetails.DocumentType, command?.MessageType);
            Assert.Equal(CimFormat.Xml.Name, command?.RequestedFormat);
            Assert.NotEqual(string.Empty, command?.FailureDescription);
            Assert.Equal(reason, command?.Reason);
        }

        private AssertQueuedCommand AssertCommand<TCommand>()
        {
            return AssertQueuedCommand.QueuedCommand<TCommand>(
                GetService<IDbConnectionFactory>(),
                GetService<InternalCommandMapper>());
        }
    }
}
