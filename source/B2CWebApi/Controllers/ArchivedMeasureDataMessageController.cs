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
using Energinet.DataHub.EDI.B2CWebApi.Models.ArchivedMeasureDataMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NodaTime.Extensions;
using Actor = Energinet.DataHub.EDI.B2CWebApi.Models.Actor;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("[controller]")]
public class ArchivedMeasureDataMessageController(
    IAuditLogger auditLogger,
    IArchivedMessagesClient archivedMessagesClient)
    : ControllerBase
{
    private readonly IAuditLogger _auditLogger = auditLogger;
    private readonly IArchivedMessagesClient _archivedMessagesClient = archivedMessagesClient;

    private readonly AuditLogEntityType _affectedEntityType = AuditLogEntityType.ArchivedMeasureDataMessage;

    [HttpPost]
    [Route("search")]
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
                affectedEntityType: _affectedEntityType,
                affectedEntityKey: null,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var messageCreationPeriod = new MessageCreationPeriodDto(
            request.CreatedDuringPeriod.Start.ToInstant(),
            request.CreatedDuringPeriod.End.ToInstant());

        var cursor = request.Pagination.Cursor != null
            ? new SortingCursorDto(request.Pagination.Cursor.FieldToSortByValue, request.Pagination.Cursor.RecordId)
            : null;
        var pageSize = request.Pagination.PageSize;
        var navigationForward = request.Pagination.NavigationForward;
        var fieldToSortBy = FieldToSortByMapper.MapToFieldToSortBy(request.Pagination.SortBy);
        var directionToSortBy = DirectionToSortByMapper.MapToDirectionToSortBy(request.Pagination.DirectionToSortBy);

        var sender = request.Sender != null
            ? new BuildingBlocks.Domain.Models.Actor(ActorNumber.Create(request.Sender!.ActorNumber), ActorRoleMapper.ToActorRoleDomain(request.Sender!.ActorRole))
            : null;
        var receiver = request.Receiver != null
            ? new BuildingBlocks.Domain.Models.Actor(ActorNumber.Create(request.Receiver!.ActorNumber), ActorRoleMapper.ToActorRoleDomain(request.Receiver!.ActorRole))
            : null;
        var documentTypes = request.MeasureDataDocumentTypes?.Select(MeasureDataDocumentTypeMapper.ToDocumentType).ToList().AsReadOnly();
        var query = new GetMeteringPointMessagesQueryDto(
            new SortedCursorBasedPaginationDto(
                cursor,
                pageSize,
                navigationForward,
                fieldToSortBy,
                directionToSortBy),
            CreationPeriod: messageCreationPeriod,
            Sender: sender,
            Receiver: receiver,
            DocumentTypes: documentTypes,
            MeteringPointId: MeteringPointId.From(request.MeteringPointId));

        var result = await _archivedMessagesClient.SearchAsync(query, cancellationToken).ConfigureAwait(false);

        var messages = result.Messages.Select(
            x => new MeteringPointArchivedMessageV1(
                CursorValue: x.CursorValue,
                Id: x.Id,
                DocumentType: MeasureDataDocumentTypeMapper.ToMeteringPointDocumentType(x.DocumentType),
                Sender: new Actor(x.SenderNumber, ActorRoleMapper.ToActorRole(x.SenderRoleCode!)),
                Receiver: new Actor(x.ReceiverNumber, ActorRoleMapper.ToActorRole(x.ReceiverRoleCode!)),
                CreatedAt: x.CreatedAt.ToDateTimeOffset()));

        return Ok(new ArchivedMeasureDataMessageSearchResponseV1(messages, TotalCount: result.TotalAmountOfMessages));
    }

    [HttpGet("{id:guid}")]
    [Produces("text/plain")]
    public async Task<ActionResult> GetDocumentV1Async(Guid id, CancellationToken cancellationToken)
    {
        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.ArchivedMeasureDataMessageGet,
                activityOrigin: HttpContext.Request.GetDisplayUrl(),
                activityPayload: id,
                affectedEntityType: _affectedEntityType,
                affectedEntityKey: id.ToString(),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var archivedMessageId = new ArchivedMessageIdDto(id);
        var archivedMessageStream = await _archivedMessagesClient.GetAsync(archivedMessageId, cancellationToken).ConfigureAwait(false);

        return archivedMessageStream is null ? NoContent() : Ok(archivedMessageStream.Stream);
    }
}
