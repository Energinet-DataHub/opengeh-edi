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
using Messaging.Application.Configuration.Commands;
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

        public async Task SavedMessageSuccessfullyAsync(Uri storedMessageLocation, ClientProvidedDetails clientProvidedDetails)
        {
            ArgumentNullException.ThrowIfNull(clientProvidedDetails);

            await _commandScheduler
                .EnqueueAsync(new SendSuccessNotification(
                    clientProvidedDetails.RequestId,
                    clientProvidedDetails.IdempotencyId,
                    clientProvidedDetails.ReferenceId,
                    clientProvidedDetails.DocumentType,
                    storedMessageLocation,
                    clientProvidedDetails.RequestedFormat))
                .ConfigureAwait(false);
        }

        public async Task RequestedMessagesWasNotFoundAsync(IReadOnlyList<string> messageIds, ClientProvidedDetails clientProvidedDetails)
        {
            ArgumentNullException.ThrowIfNull(clientProvidedDetails);

            await _commandScheduler.EnqueueAsync(
                    CreateErrorResponse(
                        clientProvidedDetails,
                        $"Message(s) with the following id(s) not found {messageIds}",
                        "DatasetNotFound"))
                .ConfigureAwait(false);
        }

        public async Task RequestedDocumentFormatIsNotSupportedAsync(ClientProvidedDetails clientProvidedDetails)
        {
            ArgumentNullException.ThrowIfNull(clientProvidedDetails);

            await _commandScheduler.EnqueueAsync(
                    CreateErrorResponse(
                        clientProvidedDetails,
                        $"Format '{clientProvidedDetails.RequestedFormat}' for document type '{clientProvidedDetails.DocumentType}' is not supported.",
                        "InternalError"))
                .ConfigureAwait(false);
        }

        public Task UnknownDocumentTypeAsync(ClientProvidedDetails clientProvidedDetails)
        {
            ArgumentNullException.ThrowIfNull(clientProvidedDetails);

            return _commandScheduler.EnqueueAsync(
                CreateErrorResponse(
                    clientProvidedDetails,
                    $"Unknown document type: '{clientProvidedDetails.DocumentType}'.",
                    "InternalError"));
        }

        private static SendFailureNotification CreateErrorResponse(ClientProvidedDetails request, string failureDescription, string reason)
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
