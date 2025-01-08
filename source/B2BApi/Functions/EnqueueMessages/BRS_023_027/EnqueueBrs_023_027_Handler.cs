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

public class EnqueueBrs_023_027_Handler(ILogger<EnqueueBrs_023_027_Handler> logger) : EnqueueMessagesHandlerBase(logger)
{
    private readonly ILogger _logger = logger;

    protected override async Task EnqueueMessagesAsync(EnqueueMessagesDto enqueueMessagesDto)
    {
        _logger.LogInformation(
            "Received enqueue messages for BRS 021/023. Data: {JsonInput}",
            enqueueMessagesDto.JsonInput);

        // TODO: Deserialize to actual input type instead of object
        var input = DeserializeJsonInput<object>(enqueueMessagesDto);

        // TODO: Call actual logic that enqueues messages (starts orchestration)
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
