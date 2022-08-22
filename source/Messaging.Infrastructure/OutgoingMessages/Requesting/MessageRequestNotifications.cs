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
using Energinet.DataHub.MessageHub.Model.Model;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages.Requesting;

namespace Messaging.Infrastructure.OutgoingMessages.Requesting
{
    public class MessageRequestNotifications : IMessageRequestNotifications
    {
        private readonly MessageRequestContext _messageRequestContext;
        private readonly ICommandScheduler _commandScheduler;

        public MessageRequestNotifications(
            MessageRequestContext messageRequestContext,
            ICommandScheduler commandScheduler)
        {
            _messageRequestContext = messageRequestContext;
            _commandScheduler = commandScheduler;
        }

        public async Task SavedMessageSuccessfullyAsync(Uri storedMessageLocation)
        {
            var request = GetRequest();

            await _commandScheduler
                .EnqueueAsync(new SendSuccessNotification(
                    request.RequestId,
                    request.IdempotencyId,
                    request.DataAvailableNotificationReferenceId,
                    request.MessageType.Value,
                    storedMessageLocation))
                .ConfigureAwait(false);
        }

        public async Task RequestedMessagesWasNotFoundAsync(IReadOnlyList<string> messageIds)
        {
            var request = GetRequest();

            await _commandScheduler.EnqueueAsync(new SendFailureNotification(
                        request.RequestId,
                        request.IdempotencyId,
                        $"Message(s) with the following id(s) not found {messageIds}",
                        "DatasetNotFound",
                        request.DataAvailableNotificationReferenceId,
                        request.MessageType.Value))
                .ConfigureAwait(false);
        }

        public async Task RequestedDocumentFormatIsNotSupportedAsync(string documentFormat, string documentType)
        {
            var request = GetRequest();

            await _commandScheduler.EnqueueAsync(new SendFailureNotification(
                    request.RequestId,
                    request.IdempotencyId,
                    $"Format '{documentFormat}' for document type '{documentType}' is not supported.",
                    "InternalError",
                    request.DataAvailableNotificationReferenceId,
                    request.MessageType.Value))
                .ConfigureAwait(false);
        }

        private DataBundleRequestDto GetRequest()
        {
            if (_messageRequestContext.DataBundleRequestDto is null)
            {
                throw new InvalidOperationException($"Data request DTO is null.");
            }

            return _messageRequestContext.DataBundleRequestDto;
        }
    }
}
