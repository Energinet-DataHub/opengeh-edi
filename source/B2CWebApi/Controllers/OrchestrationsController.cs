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

using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.EDI.AuditLog;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Newtonsoft.Json;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class OrchestrationsController(
    ILogger<OrchestrationsController> logger,
    IDurableClientFactory durableClientFactory,
    IUserContext<FrontendUser> userContext,
    IAuditLogger auditLogger) : ControllerBase
{
    private const string CalculationManageRole = "calculations:manage";

    private readonly ILogger<OrchestrationsController> _logger = logger;
    private readonly IDurableClientFactory _durableClientFactory = durableClientFactory;
    private readonly IUserContext<FrontendUser> _userContext = userContext;
    private readonly IAuditLogger _auditLogger = auditLogger;

    /// <summary>
    /// Get orchestrations
    /// </summary>
    /// <param name="from">Optional parameter to only get orchestrations after this value</param>
    /// <returns>
    /// Returns a <see cref="OrchestrationStatusQueryResult"/>. If <paramref name="from"/> is included
    /// it will return all orchestrations after this value. If <paramref name="from"/> is not included then
    /// it will return all pending and running orchestrations.
    /// </returns>
    [HttpGet]
    [ProducesResponseType<OrchestrationStatusQueryResult>(200, "application/json")]
    [Authorize(Roles = CalculationManageRole)]
    public async Task<IActionResult> IndexAsync(DateTime? from)
    {
        await _auditLogger.LogAsync(
                AuditLogId.New(),
                AuditLogActivity.OrchestrationsSearch,
                HttpContext.Request.GetDisplayUrl(),
                from?.ToString("O"),
                AuditLogEntityType.Orchestration,
                "orchestrations-search")
            .ConfigureAwait(false);

        AdminUserGuard();

        var durableClient = _durableClientFactory.CreateClient();

        var query = new OrchestrationStatusQueryCondition { ShowInput = true, };

        if (from is not null)
        {
            query.CreatedTimeFrom = from.Value;
        }
        else
        {
            query.RuntimeStatus =
            [
                OrchestrationRuntimeStatus.Pending,
                OrchestrationRuntimeStatus.Running,
            ];
        }

        var runningOrchestrations = await durableClient
            .ListInstancesAsync(query, CancellationToken.None)
            .ConfigureAwait(false);

        return Content(JsonConvert.SerializeObject(runningOrchestrations), "application/json");
    }

    /// <summary>
    /// Get status of orchestration with a given instance id
    /// </summary>
    /// <param name="id">The instance id of the orchestration to get status for</param>
    [HttpGet("{id}")]
    [ProducesResponseType<DurableOrchestrationStatus>(200, "application/json")]
    [Authorize(Roles = CalculationManageRole)]
    public async Task<IActionResult> IndexAsync(string id)
    {
        await _auditLogger.LogAsync(
                AuditLogId.New(),
                AuditLogActivity.OrchestrationsGet,
                HttpContext.Request.GetDisplayUrl(),
                id,
                AuditLogEntityType.Orchestration,
                id)
            .ConfigureAwait(false);

        AdminUserGuard();

        var durableClient = _durableClientFactory.CreateClient();
        var orchestrationStatus = await durableClient
            .GetStatusAsync(id, true, true, true)
            .ConfigureAwait(false);

        return Content(JsonConvert.SerializeObject(orchestrationStatus), "application/json");
    }

    /// <summary>
    /// Terminate a running orchestration
    /// </summary>
    /// <param name="id">The instance id of the orchestration to terminate</param>
    /// <param name="reason">The reasons for the termination</param>
    [HttpPost("{id}/terminate")]
    [Authorize(Roles = CalculationManageRole)]
    public async Task<IActionResult> TerminateAsync(string id, string reason)
    {
        await _auditLogger.LogAsync(
                AuditLogId.New(),
                AuditLogActivity.OrchestrationsTerminate,
                HttpContext.Request.GetDisplayUrl(),
                new { id, reason },
                AuditLogEntityType.Orchestration,
                id)
            .ConfigureAwait(false);

        AdminUserGuard();

        var currentUser = _userContext.CurrentUser;
        _logger.LogWarning(
            "Terminating orchestration \"{OrchestrationId}\" with reason \"{Reason}\". Terminated by user id: {UserId}, actor number: {ActorNumber}, actor id: {ActorId}",
            id.Replace(Environment.NewLine, string.Empty), // Replace new lines to avoid log injection
            reason.Replace(Environment.NewLine, string.Empty), // Replace new lines to avoid log injection,
            currentUser.UserId,
            currentUser.ActorNumber,
            currentUser.ActorId);

        var durableClient = _durableClientFactory.CreateClient();
        await durableClient
            .TerminateAsync(id, reason)
            .ConfigureAwait(false);

        return Accepted();
    }

    private void AdminUserGuard()
    {
        var user = _userContext.CurrentUser;

        if (user.ActorId.ToString() != "00000000-0000-0000-0000-000000000001")
            throw new UnauthorizedAccessException($"User actor id ({user.ActorId}) is invalid for this action");

        if (user.ActorNumber != "5790001330583")
            throw new UnauthorizedAccessException($"User actor number ({user.ActorNumber}) is invalid for this action");

        if (user.MarketRole != "DataHubAdministrator")
            throw new UnauthorizedAccessException($"User market role ({user.MarketRole}) is invalid for this action");

        if (user.Roles.Contains(CalculationManageRole))
            throw new UnauthorizedAccessException($"User roles ({string.Join(", ", user.Roles)}) are invalid for this action");
    }
}
