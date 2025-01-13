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

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_028;

/// <summary>
/// Service Bus Trigger to enqueue messages for BRS-028 (RequestWholesaleServices),
/// received from the Process Manager subsystem.
/// </summary>
/// <param name="logger"></param>
/// <param name="handler"></param>
public class EnqueueTrigger_Brs_028(
    ILogger<EnqueueTrigger_Brs_028> logger,
    EnqueueHandler_Brs_028_V1 handler)
{
    private readonly ILogger<EnqueueTrigger_Brs_028> _logger = logger;
    private readonly EnqueueHandler_Brs_028_V1 _handler = handler;

    [Function(nameof(EnqueueTrigger_Brs_028))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            $"%{EdiTopicOptions.SectionName}:{nameof(EdiTopicOptions.Name)}%",
            $"%{EdiTopicOptions.SectionName}:{nameof(EdiTopicOptions.EnqueueBrs_028_SubscriptionName)}%",
            Connection = ServiceBusNamespaceOptions.SectionName)]
        ServiceBusReceivedMessage message)
    {
        _logger.LogInformation("Enqueue BRS-028 messages service bus message received");

        await _handler.EnqueueAsync(message)
            .ConfigureAwait(false);
    }
}
