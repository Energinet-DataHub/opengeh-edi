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

using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

public sealed class EnqueueHandler_Brs_021_Forward_Metered_Data_V1(
    IOutgoingMessagesClient outgoingMessagesClient,
    ILogger<EnqueueHandler_Brs_021_Forward_Metered_Data_V1> logger)
    : EnqueueActorMessagesValidatedHandlerBase<MeteredDataForMeteringPointAcceptedV1, MeteredDataForMeteringPointRejectedV1>(logger)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly ILogger _logger = logger;

    protected override async Task EnqueueAcceptedMessagesAsync(MeteredDataForMeteringPointAcceptedV1 acceptedData)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 021. Data: {0}",
            acceptedData);

        // TODO: Call actual logic that enqueues accepted messages instead
        await Task.CompletedTask.ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(MeteredDataForMeteringPointRejectedV1 rejectedData)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 021. Data: {0}",
            rejectedData);

        // TODO: Call actual logic that enqueues accepted messages instead
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
