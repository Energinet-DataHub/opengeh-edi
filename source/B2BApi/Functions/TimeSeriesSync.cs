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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.B2BApi.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions;

/// <summary>
/// Service Bus Trigger to process time series synchronization messages from DataHub 2.
/// </summary>
public class TimeSeriesSync(ILogger<TimeSeriesSync> logger)
{
    private readonly ILogger<TimeSeriesSync> _logger = logger;

    [Function(nameof(TimeSeriesSync))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            $"%{MigrationOptions.SectionName}:{nameof(MigrationOptions.TopicName)}%",
            $"%{MigrationOptions.SectionName}:{nameof(MigrationOptions.TimeSeriesSync_SubscriptionName)}%",
            Connection = ServiceBusNamespaceOptions.SectionName)]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enqueue BRS-026 messages service bus message received");

        return await Task.CompletedTask;
    }
}
