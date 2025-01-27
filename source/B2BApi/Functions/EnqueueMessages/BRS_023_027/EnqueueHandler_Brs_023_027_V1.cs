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

using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027;

/// <summary>
/// Enqueue messages for BRS-023 (NotifyAggregatedMeasureData) and BRS-027 (NotifyWholesaleServices).
/// The <see cref="EnqueueMessagesDto"/><see cref="EnqueueMessagesDto.JsonInput"/> must be of type <see cref="object"/>.
/// </summary>
/// <param name="logger"></param>
public class EnqueueHandler_Brs_023_027_V1(
    ILogger<EnqueueHandler_Brs_023_027_V1> logger)
    : EnqueueActorMessagesHandlerBase(logger)
{
    private readonly ILogger _logger = logger;

    protected override async Task EnqueueActorMessagesV1Async(EnqueueActorMessagesV1 enqueueActorMessages)
    {
        _logger.LogInformation(
            "Received enqueue actor messages for BRS 023/027. Data: {Data}",
            enqueueActorMessages.Data);

        // TODO: Deserialize to actual input type instead of object (replace "object" type in summary as well)
        var input = enqueueActorMessages.ParseData<object>();

        // TODO: Call actual logic that enqueues messages (starts orchestration)
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
