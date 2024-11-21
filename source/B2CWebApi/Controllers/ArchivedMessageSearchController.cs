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
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.B2CWebApi.Mappers;
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
    private readonly ILogger<ArchivedMessageSearchController> _logger;

    public ArchivedMessageSearchController(
        IArchivedMessagesClient archivedMessagesClient,
        IAuditLogger auditLogger,
        ILogger<ArchivedMessageSearchController> logger)
    {
        _archivedMessagesClient = archivedMessagesClient;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    [ApiVersion("2.0")]
    [HttpPost]
    [ProducesResponseType(typeof(ArchivedMessageSearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult> RequestAsync(
        SearchArchivedMessagesRequest request,
        CancellationToken cancellationToken)
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
            ? new ArchivedMessages.Interfaces.Models.MessageCreationPeriodDto(
                request.SearchCriteria.CreatedDuringPeriod.Start.ToInstant(),
                request.SearchCriteria.CreatedDuringPeriod.End.ToInstant())
            : null;

        var cursor = request.Pagination.Cursor != null
            ? new SortingCursorDto(request.Pagination.Cursor.FieldToSortByValue, request.Pagination.Cursor.RecordId)
            : null;
        var pageSize = request.Pagination.PageSize;
        var navigationForward = request.Pagination.NavigationForward;
        var fieldToSortBy = FieldToSortByMapper.MapToFieldToSortBy(request.Pagination.SortBy);
        var directionToSortBy = DirectionToSortByMapper.MapToDirectionToSortBy(request.Pagination.DirectionToSortBy);

        var query = new GetMessagesQueryDto(
            new SortedCursorBasedPaginationDto(
                cursor,
                pageSize,
                navigationForward,
                fieldToSortBy,
                directionToSortBy),
            messageCreationPeriod,
            request.SearchCriteria.MessageId,
            request.SearchCriteria.SenderNumber,
            null,
            request.SearchCriteria.ReceiverNumber,
            null,
            request.SearchCriteria.DocumentTypes,
            request.SearchCriteria.BusinessReasons,
            request.SearchCriteria.IncludeRelatedMessages);

        var result = await _archivedMessagesClient.SearchAsync(query, cancellationToken).ConfigureAwait(false);

        var messages = result.Messages.Select(
            x => new ArchivedMessageResultV2(
                x.RecordId,
                x.Id.ToString(),
                x.MessageId,
                x.DocumentType,
                x.SenderNumber,
                x.ReceiverNumber,
                x.CreatedAt.ToDateTimeOffset(),
                x.BusinessReason));
        return Ok(new ArchivedMessageSearchResponse(messages, TotalCount: result.TotalAmountOfMessages));
    }

    [ApiVersion("3.0")]
    [HttpPost]
    [ProducesResponseType(typeof(ArchivedMessageSearchResponseV3), StatusCodes.Status200OK)]
    public async Task<ActionResult> RequestV3Async(
        SearchArchivedMessagesRequestV3 request,
        CancellationToken cancellationToken)
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
            ? new ArchivedMessages.Interfaces.Models.MessageCreationPeriodDto(
                request.SearchCriteria.CreatedDuringPeriod.Start.ToInstant(),
                request.SearchCriteria.CreatedDuringPeriod.End.ToInstant())
            : null;

        var cursor = request.Pagination.Cursor != null
            ? new SortingCursorDto(request.Pagination.Cursor.FieldToSortByValue, request.Pagination.Cursor.RecordId)
            : null;
        var pageSize = request.Pagination.PageSize;
        var navigationForward = request.Pagination.NavigationForward;
        var fieldToSortBy = FieldToSortByMapper.MapToFieldToSortBy(request.Pagination.SortBy);
        var directionToSortBy = DirectionToSortByMapper.MapToDirectionToSortBy(request.Pagination.DirectionToSortBy);

        var query = new GetMessagesQueryDto(
            new SortedCursorBasedPaginationDto(
                cursor,
                pageSize,
                navigationForward,
                fieldToSortBy,
                directionToSortBy),
            messageCreationPeriod,
            request.SearchCriteria.MessageId,
            request.SearchCriteria.SenderNumber,
            ActorRoleMapper.ToActorRoleCode(request.SearchCriteria.SenderRole),
            request.SearchCriteria.ReceiverNumber,
            ActorRoleMapper.ToActorRoleCode(request.SearchCriteria.ReceiverRole),
            DocumentTypeMapper.FromDocumentTypes(request.SearchCriteria.DocumentTypes),
            request.SearchCriteria.BusinessReasons,
            request.SearchCriteria.IncludeRelatedMessages);

        var result = await _archivedMessagesClient.SearchAsync(query, cancellationToken).ConfigureAwait(false);

        try
        {
            var messages = result.Messages
                .Select(
                    x => new ArchivedMessageResultV3(
                        x.RecordId,
                        x.Id.ToString(),
                        x.MessageId,
                        DocumentTypeMapper.ToDocumentType(x.DocumentType),
                        x.SenderNumber,
                        ActorRoleMapper.ToActorRole(x.SenderRoleCode),
                        x.ReceiverNumber,
                        ActorRoleMapper.ToActorRole(x.ReceiverRoleCode),
                        x.CreatedAt.ToDateTimeOffset(),
                        x.BusinessReason));
            return Ok(new ArchivedMessageSearchResponseV3(messages, TotalCount: result.TotalAmountOfMessages));
        }
        catch (ArgumentNullException ex)
        {
            var containsANullValue = result.Messages.Any(x => x == null);
            _logger.LogError(ex, "An ArgumentNullException occurred while processing messages. TotalCount: {TotalCount}, ContainsANullableValue: {ContainsANullValue} Result: {Result}, Messages: {Messages}", result.TotalAmountOfMessages, containsANullValue, result, result?.Messages);
            throw;
        }
    }
}
