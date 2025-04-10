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

using Asp.Versioning;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.B2CWebApi.Models.ArchivedMeasureDataMessages;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ArchivedMeasureDataMessageSearchController(
    IAuditLogger auditLogger)
    : ControllerBase
{
    private readonly IAuditLogger _auditLogger = auditLogger;

    [ApiVersion("1.0")]
    [HttpPost]
    [ProducesResponseType(typeof(ArchivedMeasureDataMessageSearchResponseV1), StatusCodes.Status200OK)]
    public async Task<ActionResult> SearchV1Async(
        ArchivedMeasureDataMessageSearchCriteria request,
        CancellationToken cancellationToken)
    {
        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.ArchivedMeasureDataMessageSearch,
                activityOrigin: HttpContext.Request.GetDisplayUrl(),
                activityPayload: request,
                affectedEntityType: AuditLogEntityType.ArchivedMessage,
                affectedEntityKey: null,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var message = new ArchivedMeasureDataMessageV1(
            Id: Guid.NewGuid(),
            DocumentType: MeasureDataDocumentType.NotifyValidatedMeasureData,
            CreatedAt: DateTimeOffset.UtcNow,
            Sender: request.Sender ?? new Actor(ActorRole: ActorRole.DataHubAdministrator, ActorNumber: "1234567890123"),
            Receiver: request.Receiver ?? new Actor(ActorRole: ActorRole.MeteredDataResponsible, ActorNumber: "0987654321098"));

        var messages = new List<ArchivedMeasureDataMessageV1>()
        {
            message,
        };

        return Ok(new ArchivedMeasureDataMessageSearchResponseV1(messages, messages.Count));
    }
}
