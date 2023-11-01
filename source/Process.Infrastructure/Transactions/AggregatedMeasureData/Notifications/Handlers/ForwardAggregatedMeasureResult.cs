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
using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.Application.Configuration.Commands;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Commands;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Notifications.Handlers;

public class ForwardAggregatedMeasureResult : INotificationHandler<AggregatedMeasureDataResultIsAvailable>
{
    private readonly IMediator _mediator;

    public ForwardAggregatedMeasureResult(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(AggregatedMeasureDataResultIsAvailable notification, CancellationToken cancellationToken)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        //TODO: looks like the handler is not catching this call
        await _mediator.Send(new EnqueueMessageCommand(notification.Message), cancellationToken).ConfigureAwait(false);
    }
}
