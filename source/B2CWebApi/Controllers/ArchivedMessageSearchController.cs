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

using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.AuditLog;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ArchivedMessageSearchController : ControllerBase
{
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly IAuditLogger _auditLogger;

    public ArchivedMessageSearchController(
        IArchivedMessagesClient archivedMessagesClient,
        IAuditLogger auditLogger)
    {
        _archivedMessagesClient = archivedMessagesClient;
        _auditLogger = auditLogger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ArchivedMessageResult[]), StatusCodes.Status200OK)]
    public async Task<ActionResult> RequestAsync(SearchArchivedMessagesCriteria request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _auditLogger.LogAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.ArchivedMessagesSearch,
                activityOrigin: HttpContext.Request.GetDisplayUrl(),
                activityPayload: request,
                affectedEntityType: AuditLogEntityType.ArchivedMessage,
                affectedEntityKey: null)
            .ConfigureAwait(false);

        var query = new GetMessagesQuery
        {
            CreationPeriod = request.CreatedDuringPeriod is not null
                ? new EDI.ArchivedMessages.Interfaces.MessageCreationPeriod(
                    request.CreatedDuringPeriod.Start.ToInstant(),
                    request.CreatedDuringPeriod.End.ToInstant())
                : null,
            MessageId = request.MessageId,
            SenderNumber = request.SenderNumber,
            ReceiverNumber = request.ReceiverNumber,
            DocumentTypes = request.DocumentTypes,
            BusinessReasons = request.BusinessReasons,
            IncludeRelatedMessages = request.IncludeRelatedMessages,
        };

        var result = await _archivedMessagesClient.SearchAsync(query, cancellationToken).ConfigureAwait(false);

        return Ok(result.Messages.Select(x => new ArchivedMessageResult(
            x.Id.ToString(),
            x.MessageId,
            x.DocumentType,
            x.SenderNumber,
            x.ReceiverNumber,
            x.CreatedAt.ToDateTimeOffset(),
            x.BusinessReason)));
    }
}
