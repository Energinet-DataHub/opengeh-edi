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

using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.Wholesale;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications.Handlers;

public sealed class
    NotifyWholesaleThatAggregatedMeasureDataIsRequestedHandler : INotificationHandler<
    NotifyWholesaleThatAggregatedMeasureDataIsRequested>
{
    private readonly WholesaleInboxClient _wholesaleInboxClient;

    public NotifyWholesaleThatAggregatedMeasureDataIsRequestedHandler(
        WholesaleInboxClient wholesaleInboxClient)
    {
        _wholesaleInboxClient = wholesaleInboxClient;
    }

    public async Task Handle(
        NotifyWholesaleThatAggregatedMeasureDataIsRequested notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        await _wholesaleInboxClient.SendProcessAsync(notification.Process, cancellationToken).ConfigureAwait(false);
    }
}
