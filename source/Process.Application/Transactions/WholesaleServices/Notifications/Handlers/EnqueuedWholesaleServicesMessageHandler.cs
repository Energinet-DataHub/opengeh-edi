﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices.ProcessEvents;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Notifications.Handlers;

public class EnqueuedWholesaleServicesMessageHandler : INotificationHandler<EnqueuedWholesaleServicesEvent>
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;

    public EnqueuedWholesaleServicesMessageHandler(IOutgoingMessagesClient outgoingMessagesClient)
    {
        _outgoingMessagesClient = outgoingMessagesClient;
    }

    public async Task Handle(EnqueuedWholesaleServicesEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        await _outgoingMessagesClient.EnqueueAsync(notification.AcceptedWholesaleServices, cancellationToken).ConfigureAwait(false);
    }
}
