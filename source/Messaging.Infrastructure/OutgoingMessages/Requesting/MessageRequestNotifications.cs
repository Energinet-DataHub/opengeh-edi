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
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages.Requesting;

namespace Messaging.Infrastructure.OutgoingMessages.Requesting
{
    public class MessageRequestNotifications : IMessageRequestNotifications
    {
        private readonly ICommandScheduler _commandScheduler;

        public MessageRequestNotifications(
            ICommandScheduler commandScheduler)
        {
            _commandScheduler = commandScheduler;
        }

        public async Task SavedMessageSuccessfullyAsync(Uri storedMessageLocation, MessageRequest messageRequest)
        {
            ArgumentNullException.ThrowIfNull(messageRequest);

            await _commandScheduler
                .EnqueueAsync(new SendSuccessNotification(
                    messageRequest.RequestId,
                    messageRequest.IdempotencyId,
                    messageRequest.ReferenceId,
                    messageRequest.DocumentType,
                    storedMessageLocation,
                    messageRequest.RequestedFormat))
                .ConfigureAwait(false);
        }

        public async Task RequestedMessagesWasNotFoundAsync(IReadOnlyList<string> messageIds, MessageRequest messageRequest)
        {
            ArgumentNullException.ThrowIfNull(messageRequest);

            await _commandScheduler.EnqueueAsync(
                    CreateErrorResponse(
                        messageRequest,
                        $"Message(s) with the following id(s) not found {messageIds}",
                        "DatasetNotFound"))
                .ConfigureAwait(false);
        }

        public async Task RequestedDocumentFormatIsNotSupportedAsync(string documentFormat, string documentType, MessageRequest messageRequest)
        {
            ArgumentNullException.ThrowIfNull(messageRequest);

            await _commandScheduler.EnqueueAsync(
                    CreateErrorResponse(
                        messageRequest,
                        $"Format '{documentFormat}' for document type '{documentType}' is not supported.",
                        "InternalError"))
                .ConfigureAwait(false);
        }

        private static SendFailureNotification CreateErrorResponse(MessageRequest request, string failureDescription, string reason)
        {
            return new SendFailureNotification(
                request.RequestId,
                request.IdempotencyId,
                failureDescription,
                reason,
                request.ReferenceId,
                request.DocumentType,
                request.RequestedFormat);
        }
    }
}
