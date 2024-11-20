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

using Energinet.DataHub.ProcessManagement.Core.Application.Orchestration;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Api.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ProcessManager.Api;

internal class CancelScheduledOrchestrationInstanceTrigger(
    ILogger<CancelScheduledOrchestrationInstanceTrigger> logger,
    ICancelScheduledOrchestrationInstanceCommand command)
{
    private readonly ILogger _logger = logger;
    private readonly ICancelScheduledOrchestrationInstanceCommand _command = command;

    /// <summary>
    /// Cancel a scheduled orchestration instance.
    /// </summary>
    [Function(nameof(CancelScheduledOrchestrationInstanceTrigger))]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "processmanager/orchestrationinstance/cancel")]
        HttpRequest httpRequest,
        [FromBody]
        CancelScheduledOrchestrationInstanceCommand dto,
        FunctionContext executionContext)
    {
        await _command
            .CancelScheduledOrchestrationInstanceAsync(
                new UserIdentity(new UserId(dto.UserIdentity.UserId), new ActorId(dto.UserIdentity.ActorId)),
                new OrchestrationInstanceId(dto.Id))
            .ConfigureAwait(false);

        return new OkResult();
    }
}
