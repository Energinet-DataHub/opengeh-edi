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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MessageHub.Model.Model;
using MediatR;
using Messaging.Application.Common;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.IncomingMessages;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Requesting;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.OutgoingMessages.Requesting;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;

namespace Messaging.IntegrationTests.Application.OutgoingMessages.Requesting
{
    public class RequestMessagesTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly MessageStorageSpy _messageStorage;
        private MessageRequestContext _messageRequestContext;

        public RequestMessagesTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _messageStorage = (MessageStorageSpy)GetService<IMessageStorage>();
            _messageRequestContext = GetService<MessageRequestContext>();
        }

        [Fact]
        public async Task Message_is_dispatched_on_request()
        {
            var incomingMessage1 = await MessageArrived().ConfigureAwait(false);
            var incomingMessage2 = await MessageArrived().ConfigureAwait(false);
            var outgoingMessage1 = _outgoingMessageStore.GetByOriginalMessageId(incomingMessage1.Message.MessageId)!;
            var outgoingMessage2 = _outgoingMessageStore.GetByOriginalMessageId(incomingMessage2.Message.MessageId)!;

            var requestedMessageIds = new List<string> { outgoingMessage1.Id.ToString(), outgoingMessage2.Id.ToString(), };
            await RequestMessages(requestedMessageIds.AsReadOnly()).ConfigureAwait(false);

            _messageStorage.MessageHasBeenSavedInStorage();
            var command = GetQueuedNotification<SendSuccessNotification>();
            Assert.NotNull(command);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.RequestId, command?.RequestId);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.IdempotencyId, command?.IdempotencyId);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.DataAvailableNotificationReferenceId, command?.ReferenceId);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.MessageType.Value, command?.MessageType);
            Assert.NotNull(command?.MessageStorageLocation);
        }

        [Fact]
        public async Task Requested_message_ids_must_exist()
        {
            var nonExistingMessage = new List<string> { Guid.NewGuid().ToString() };

            await RequestMessages(nonExistingMessage.AsReadOnly()).ConfigureAwait(false);

            Assert.Null(_messageStorage.SavedMessage);
            var command = GetQueuedNotification<SendFailureNotification>();
            Assert.NotNull(command);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.RequestId, command?.RequestId);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.DataAvailableNotificationReferenceId, command?.ReferenceId);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.MessageType.Value, command?.MessageType);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.IdempotencyId, command?.IdempotencyId);
            Assert.NotEqual(string.Empty, command?.FailureDescription);
            Assert.Equal("DatasetNotFound", command?.Reason);
        }

        [Fact]
        public async Task Respond_with_error_if_requested_message_format_can_not_be_handled()
        {
            GivenTheRequestedDocumentFormatIsNotSupported();

            var incomingMessage = await MessageArrived().ConfigureAwait(false);
            var outgoingMessage = _outgoingMessageStore.GetByOriginalMessageId(incomingMessage.Message.MessageId)!;

            var requestedMessageIds = new List<string> { outgoingMessage.Id.ToString(), };
            await RequestMessages(requestedMessageIds.AsReadOnly()).ConfigureAwait(false);

            Assert.Null(_messageStorage.SavedMessage);
            var command = GetQueuedNotification<SendFailureNotification>();
            Assert.NotNull(command);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.RequestId, command?.RequestId);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.DataAvailableNotificationReferenceId, command?.ReferenceId);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.MessageType.Value, command?.MessageType);
            Assert.Equal(_messageRequestContext.DataBundleRequestDto?.IdempotencyId, command?.IdempotencyId);
            Assert.NotEqual(string.Empty, command?.FailureDescription);
            Assert.Equal("InternalError", command?.Reason);
        }

        private static IncomingMessageBuilder MessageBuilder()
        {
            return new IncomingMessageBuilder()
                .WithProcessType(ProcessType.MoveIn.Code);
        }

        private TNotification? GetQueuedNotification<TNotification>()
        {
            var sql = "SELECT Data FROM b2b.QueuedInternalCommands WHERE Type = @CommandType";
            var commandData = GetService<IDbConnectionFactory>()
                .GetOpenConnection()
                .ExecuteScalar<string>(
                    sql,
                    new { CommandType = typeof(TNotification).AssemblyQualifiedName, });

            return JsonSerializer.Deserialize<TNotification>(commandData);
        }

        private async Task<IncomingMessage> MessageArrived()
        {
            var incomingMessage = MessageBuilder()
                .Build();
            await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
            return incomingMessage;
        }

        private Task RequestMessages(IEnumerable<string> messageIds)
        {
            _messageRequestContext = GetService<MessageRequestContext>();
            _messageRequestContext.SetMessageRequest(new DataBundleRequestDto(
                Guid.Empty,
                string.Empty,
                string.Empty,
                new MessageTypeDto(string.Empty),
                ResponseFormat.Xml,
                1));
            return GetService<IMediator>().Send(new RequestMessages(messageIds.ToList(), CimFormat.Xml.Name));
        }

        private void GivenTheRequestedDocumentFormatIsNotSupported()
        {
            RegisterInstance(new DocumentFactory(new List<DocumentWriter>()));
        }
    }
}
