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

using System.Globalization;
using Energinet.DataHub.ProcessManagement.Core.Application;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Api.Mappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.ProcessManager.Api;

internal class SearchOrchestrationInstancesTrigger(
    ILogger<SearchOrchestrationInstancesTrigger> logger,
    IOrchestrationInstanceRepository repository)
{
    private readonly ILogger _logger = logger;
    private readonly IOrchestrationInstanceRepository _repository = repository;

    [Function(nameof(SearchOrchestrationInstancesTrigger))]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "processmanager/orchestrationinstances/{name}/{version:int?}")]
        HttpRequest httpRequest,
        string name,
        int? version,
        FunctionContext executionContext)
    {
        var lifecycleState =
            Enum.TryParse<OrchestrationInstanceLifecycleStates>(httpRequest.Query["lifecycleState"], ignoreCase: true, out var lifecycleStateResult)
            ? lifecycleStateResult
            : (OrchestrationInstanceLifecycleStates?)null;
        var terminationState =
            Enum.TryParse<OrchestrationInstanceTerminationStates>(httpRequest.Query["terminationState"], ignoreCase: true, out var terminationStateResult)
            ? terminationStateResult
            : (OrchestrationInstanceTerminationStates?)null;

        // DateTimeOffset values must be in "round-trip" ("o"/"O") format to be parsed correctly
        // See https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#the-round-trip-o-o-format-specifier
        var startedAtOrLater =
            DateTimeOffset.TryParse(httpRequest.Query["startedAtOrLater"], CultureInfo.InvariantCulture, out var startedAtOrLaterResult)
            ? Instant.FromDateTimeOffset(startedAtOrLaterResult)
            : (Instant?)null;
        var terminatedAtOrEarlier =
            DateTimeOffset.TryParse(httpRequest.Query["terminatedAtOrEarlier"], CultureInfo.InvariantCulture, out var terminatedAtOrEarlierResult)
            ? Instant.FromDateTimeOffset(terminatedAtOrEarlierResult)
            : (Instant?)null;

        var orchestrationInstances = await _repository
            .SearchAsync(name, version, lifecycleState, terminationState, startedAtOrLater, terminatedAtOrEarlier)
            .ConfigureAwait(false);

        var dto = orchestrationInstances.MapToDto();
        return new OkObjectResult(dto);
    }
}
