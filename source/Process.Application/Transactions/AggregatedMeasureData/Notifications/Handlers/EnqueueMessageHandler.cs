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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Contracts;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications.Handlers;

public class EnqueueMessageHandler : INotificationHandler<EnqueueMessageEvent>
{
    private readonly IMessageEnqueuer _messageEnqueuer;

    public EnqueueMessageHandler(IMessageEnqueuer messageEnqueuer)
    {
        _messageEnqueuer = messageEnqueuer ?? throw new ArgumentNullException(nameof(messageEnqueuer));
    }

    public async Task Handle(EnqueueMessageEvent notification, CancellationToken cancellationToken)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        await _messageEnqueuer.EnqueueAsync(notification.OutgoingMessageDto).ConfigureAwait(false);
    }
}
