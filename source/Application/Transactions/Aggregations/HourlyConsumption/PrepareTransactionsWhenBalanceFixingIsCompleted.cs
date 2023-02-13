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
using MediatR;

namespace Application.Transactions.Aggregations.HourlyConsumption;

public class PrepareTransactionsWhenBalanceFixingIsCompleted : INotificationHandler<AggregationProcessHasCompleted>
{
    private readonly CommandSchedulerFacade _commandScheduler;

    public PrepareTransactionsWhenBalanceFixingIsCompleted(CommandSchedulerFacade commandScheduler)
    {
        _commandScheduler = commandScheduler;
    }

    public Task Handle(AggregationProcessHasCompleted notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return _commandScheduler.EnqueueAsync(new PrepareTransactions(
            notification.ResultId,
            notification.GridAreaCode,
            new Period(notification.PeriodStartDate, notification.PeriodEndDate)));
    }
}
