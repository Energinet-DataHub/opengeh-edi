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
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NodaTime.Extensions;
using DirectionToSortBy = Energinet.DataHub.EDI.ArchivedMessages.Interfaces.DirectionToSortBy;
using FieldToSortBy = Energinet.DataHub.EDI.ArchivedMessages.Interfaces.FieldToSortBy;

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

    [ApiVersion("1.0")]
    [HttpPost]
    [ProducesResponseType(typeof(ArchivedMessageResult[]), StatusCodes.Status200OK)]
    public async Task<ActionResult> RequestAsync(SearchArchivedMessagesCriteria request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.ArchivedMessagesSearch,
                activityOrigin: HttpContext.Request.GetDisplayUrl(),
                activityPayload: request,
                affectedEntityType: AuditLogEntityType.ArchivedMessage,
                affectedEntityKey: null)
            .ConfigureAwait(false);

        var messageCreationPeriod = request.CreatedDuringPeriod is not null
            ? new EDI.ArchivedMessages.Interfaces.MessageCreationPeriod(
                request.CreatedDuringPeriod.Start.ToInstant(),
                request.CreatedDuringPeriod.End.ToInstant())
            : null;

        var pagination = new SortedCursorBasedPagination(
            pageSize: 1500000);

        var query = new GetMessagesQuery(
            pagination,
            messageCreationPeriod,
            request.MessageId,
            request.SenderNumber,
            request.ReceiverNumber,
            request.DocumentTypes,
            request.BusinessReasons,
            request.IncludeRelatedMessages);

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

    [ApiVersion("2.0")]
    [HttpPost]
    [ProducesResponseType(typeof(ArchivedMessageResultV2[]), StatusCodes.Status200OK)]
    public async Task<ActionResult> RequestAsync(SearchArchivedMessagesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.ArchivedMessagesSearch,
                activityOrigin: HttpContext.Request.GetDisplayUrl(),
                activityPayload: request,
                affectedEntityType: AuditLogEntityType.ArchivedMessage,
                affectedEntityKey: null)
            .ConfigureAwait(false);
        var messageCreationPeriod = request.SearchCriteria.CreatedDuringPeriod is not null
            ? new ArchivedMessages.Interfaces.MessageCreationPeriod(
                request.SearchCriteria.CreatedDuringPeriod.Start.ToInstant(),
                request.SearchCriteria.CreatedDuringPeriod.End.ToInstant())
            : null;

        var cursor = request.Pagination.Cursor != null ? new SortingCursor(request.Pagination.Cursor.FieldToSortByValue, request.Pagination.Cursor.RecordId) : null;
        var pageSize = request.Pagination.PageSize;
        var navigationForward = request.Pagination.NavigationForward;
        var fieldToSortBy = MapToFieldToSortBy(request.Pagination.SortBy);
        var directionToSortBy = MapToDirectionToSortBy(request.Pagination.DirectionToSortBy);

        var query = new GetMessagesQuery(
            new SortedCursorBasedPagination(
                cursor,
                pageSize,
                navigationForward,
                fieldToSortBy,
                directionToSortBy),
            messageCreationPeriod,
            request.SearchCriteria.MessageId,
            request.SearchCriteria.SenderNumber,
            request.SearchCriteria.ReceiverNumber,
            request.SearchCriteria.DocumentTypes,
            request.SearchCriteria.BusinessReasons,
            request.SearchCriteria.IncludeRelatedMessages);

        var result = await _archivedMessagesClient.SearchAsync(query, cancellationToken).ConfigureAwait(false);

        return Ok(result.Messages.Select(x => new ArchivedMessageResultV2(
            x.RecordId,
            x.Id.ToString(),
            x.MessageId,
            x.DocumentType,
            x.SenderNumber,
            x.ReceiverNumber,
            x.CreatedAt.ToDateTimeOffset(),
            x.BusinessReason)));
    }

    private DirectionToSortBy? MapToDirectionToSortBy(Energinet.DataHub.EDI.B2CWebApi.Models.DirectionToSortBy? directionToSortBy)
    {
        return directionToSortBy switch
        {
            Energinet.DataHub.EDI.B2CWebApi.Models.DirectionToSortBy.Ascending => DirectionToSortBy.Ascending,
            Energinet.DataHub.EDI.B2CWebApi.Models.DirectionToSortBy.Descending => DirectionToSortBy.Descending,
            _ => null,
        };
    }

    private FieldToSortBy? MapToFieldToSortBy(Energinet.DataHub.EDI.B2CWebApi.Models.FieldToSortBy? fieldToSortBy)
    {
        return fieldToSortBy switch
        {
            Energinet.DataHub.EDI.B2CWebApi.Models.FieldToSortBy.CreatedAt => FieldToSortBy.CreatedAt,
            Energinet.DataHub.EDI.B2CWebApi.Models.FieldToSortBy.MessageId => FieldToSortBy.MessageId,
            Energinet.DataHub.EDI.B2CWebApi.Models.FieldToSortBy.SenderNumber => FieldToSortBy.SenderNumber,
            Energinet.DataHub.EDI.B2CWebApi.Models.FieldToSortBy.ReceiverNumber => FieldToSortBy.ReceiverNumber,
            _ => null,
        };
    }
}
