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
using Application.Configuration.Commands;
using Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Infrastructure.Transactions.AggregatedMeasureData.Commands;
using MediatR;

namespace Infrastructure.Transactions.AggregatedMeasureData.Notifications.Handlers;

public class AcceptedAggregatedMeasureProcessIsAvailable : INotificationHandler<AggregatedMeasureProcessWasAccepted>
{
    private readonly ICommandScheduler _commandScheduler;

    public AcceptedAggregatedMeasureProcessIsAvailable(ICommandScheduler commandScheduler)
    {
        _commandScheduler = commandScheduler;
    }

    public async Task Handle(AggregatedMeasureProcessWasAccepted notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        await _commandScheduler.EnqueueAsync(new CreateAggregatedMeasureAggregationResults(notification.ProcessId.Id)).ConfigureAwait(false);
    }
}
