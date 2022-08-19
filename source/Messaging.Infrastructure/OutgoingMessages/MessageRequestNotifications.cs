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
using Messaging.Application.OutgoingMessages;

namespace Messaging.Infrastructure.OutgoingMessages
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
            if (_messageRequestContext.DataBundleRequestDto is null)
            {
                throw new InvalidOperationException($"Data request DTO is null.");
            }

            await _commandScheduler.EnqueueAsync(
                new SendMessageRequestNotification(
                    _messageRequestContext.DataBundleRequestDto,
                    storedMessageLocation)).ConfigureAwait(false);

            await _commandScheduler
                .EnqueueAsync(new SendSuccessNotification(
                    _messageRequestContext.DataBundleRequestDto.RequestId,
                    _messageRequestContext.DataBundleRequestDto.IdempotencyId,
                    _messageRequestContext.DataBundleRequestDto.DataAvailableNotificationReferenceId))
                .ConfigureAwait(false);
        }

        public async Task RequestedMessagesWasNotFoundAsync(IReadOnlyList<string> messageIds)
        {
            await _commandScheduler.EnqueueAsync(
                new SendMessageRequestNotification(
                    _messageRequestContext.DataBundleRequestDto ?? throw new InvalidOperationException(),
                    MessageRequestContext.CreateErrorDataNotFoundResponse(
                        messageIds))).ConfigureAwait(false);
        }
    }
}
