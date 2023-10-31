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
using Energinet.DataHub.EDI.Application.Configuration.Commands;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Notifications.Handlers;

public class RetryFetchingDataForAggregatedMeasureDataProcess : INotificationHandler<AggregatedMeasureDataProcessRetryFetchingData>
{
    private readonly ILogger<RetryFetchingDataForAggregatedMeasureDataProcess> _logger;
    private readonly ICommandScheduler _commandScheduler;

    public RetryFetchingDataForAggregatedMeasureDataProcess(
        ICommandScheduler commandScheduler,
        ILogger<RetryFetchingDataForAggregatedMeasureDataProcess> logger)
    {
        _logger = logger;
        _commandScheduler = commandScheduler;
    }

    public async Task Handle(AggregatedMeasureDataProcessRetryFetchingData notification, CancellationToken cancellationToken)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        _logger.LogWarning("Retrying fetching data for process {ProcessId}", notification.ProcessId.Id.ToString());
        await _commandScheduler.EnqueueAsync(new SendAggregatedMeasureRequestToWholesale(notification.ProcessId.Id)).ConfigureAwait(false);
    }
}
