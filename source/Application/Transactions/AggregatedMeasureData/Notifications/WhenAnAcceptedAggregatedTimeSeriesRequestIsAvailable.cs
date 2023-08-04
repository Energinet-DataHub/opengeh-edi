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
using Application.Configuration.Commands;
using Application.Transactions.AggregatedMeasureData.Commands;
using MediatR;

namespace Application.Transactions.AggregatedMeasureData.Notifications;

public class WhenAnAcceptedAggregatedTimeSeriesRequestIsAvailable : INotificationHandler<AggregatedTimeSeriesRequestWasAccepted>
{
    private readonly CommandSchedulerFacade _commandSchedulerFacade;

    public WhenAnAcceptedAggregatedTimeSeriesRequestIsAvailable(CommandSchedulerFacade commandSchedulerFacade)
    {
        _commandSchedulerFacade = commandSchedulerFacade;
    }

    public Task Handle(AggregatedTimeSeriesRequestWasAccepted notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return _commandSchedulerFacade.EnqueueAsync(new AcceptedAggregatedTimeSeries(notification.ProcessId, notification.AggregatedTimeSerie));
    }
}
