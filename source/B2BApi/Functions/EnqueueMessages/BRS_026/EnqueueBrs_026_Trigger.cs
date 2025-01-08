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

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;

public class EnqueueBrs_026_Trigger(
    ILogger<EnqueueBrs_026_Trigger> logger,
    EnqueueBrs_026_Handler enqueueBrs026Handler)
{
    private readonly ILogger<EnqueueBrs_026_Trigger> _logger = logger;
    private readonly EnqueueBrs_026_Handler _enqueueBrs026Handler = enqueueBrs026Handler;

    [Function(nameof(EnqueueBrs_026_Trigger))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            $"%{EdiTopicOptions.SectionName}:{nameof(EdiTopicOptions.Name)}%",
            $"%{EdiTopicOptions.SectionName}:{nameof(EdiTopicOptions.EnqueueBrs_026_SubscriptionName)}%",
            Connection = ServiceBusNamespaceOptions.SectionName)]
        ServiceBusReceivedMessage message)
    {
        _logger.LogInformation("Enqueue BRS-026 messages service bus message received");

        await _enqueueBrs026Handler.EnqueueAsync(message)
            .ConfigureAwait(false);
    }
}
