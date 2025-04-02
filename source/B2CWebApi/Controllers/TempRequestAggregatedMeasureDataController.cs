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

using System.Text;
using Asp.Versioning;
using Energinet.DataHub.Core.App.Common.Users;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using ActorRole = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ActorRole;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TempRequestAggregatedMeasureDataController : ControllerBase
{
    private const string AggregatedMeasureDataMessageType = "E74";
    private const string Electricity = "23";

    private readonly UserContext<FrontendUser> _userContext;
    private readonly IIncomingMessageClient _incomingMessageClient;
    private readonly ISerializer _serializer;
    private readonly IClock _clock;
    private readonly IAuditLogger _auditLogger;

    public TempRequestAggregatedMeasureDataController(
        UserContext<FrontendUser> userContext,
        IIncomingMessageClient incomingMessageClient,
        ISerializer serializer,
        IClock clock,
        IAuditLogger auditLogger)
    {
        _userContext = userContext;
        _incomingMessageClient = incomingMessageClient;
        _serializer = serializer;
        _clock = clock;
        _auditLogger = auditLogger;
    }

    [ApiVersion("1.0")]
    [HttpPost]
    [Authorize(Roles = "request-aggregated-measured-data:view")]
    public async Task<ActionResult> RequestAsync(RequestAggregatedMeasureDataV1 request, CancellationToken cancellationToken)
    {
        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.RequestEnergyResults,
                activityOrigin: HttpContext.Request.GetDisplayUrl(),
                activityPayload: request,
                affectedEntityType: AuditLogEntityType.RequestAggregatedMeasureData,
                affectedEntityKey: null)
            .ConfigureAwait(false);

        var currentUser = _userContext.CurrentUser;

        var message = new RequestAggregatedMeasureDataDto(
            SenderNumber: currentUser.ActorNumber,
            SenderRoleCode: MapRoleNameToCode(currentUser.MarketRole),
            ReceiverNumber: DataHubDetails.DataHubActorNumber.Value,
            ReceiverRoleCode: ActorRole.MeteredDataAdministrator.Code,
            BusinessReason: request.BusinessReason,
            MessageType: AggregatedMeasureDataMessageType,
            MessageId: MessageId.New().Value,
            CreatedAt: _clock.GetCurrentInstant().ToString(),
            BusinessType: Electricity,
            Serie:
            [
                new RequestAggregatedMeasureDataSeries(
                    Id: TransactionId.New().Value,
                    MarketEvaluationPointType: request.MeteringPointType,
                    MarketEvaluationSettlementMethod: request.MarketEvaluationSettlementMethod,
                    StartDateAndOrTimeDateTime: request.StartDateAndOrTimeDateTime,
                    EndDateAndOrTimeDateTime: request.EndDateAndOrTimeDateTime,
                    MeteringGridAreaDomainId: request.MeteringGridAreaDomainId,
                    EnergySupplierMarketParticipantId: request.EnergySupplierMarketParticipantId,
                    BalanceResponsiblePartyMarketParticipantId: request.BalanceResponsiblePartyMarketParticipantId,
                    SettlementVersion: request.SettlementVersion),
            ]);

        var responseMessage = await _incomingMessageClient.ReceiveIncomingMarketMessageAsync(
                GenerateStreamFromString(_serializer.Serialize(message)),
                DocumentFormat.Json,
                IncomingDocumentType.B2CRequestAggregatedMeasureData,
                DocumentFormat.Json,
                cancellationToken)
            .ConfigureAwait(false);

        if (responseMessage.IsErrorResponse)
        {
            return BadRequest(responseMessage.MessageBody);
        }

        return Ok(responseMessage.MessageBody);
    }

    private static IncomingMarketMessageStream GenerateStreamFromString(string jsonString)
    {
        var encoding = Encoding.UTF8;
        var byteArray = encoding.GetBytes(jsonString);
        var memoryStream = new MemoryStream(byteArray);
        return new IncomingMarketMessageStream(memoryStream);
    }

    private static string MapRoleNameToCode(string roleName)
    {
        ArgumentException.ThrowIfNullOrEmpty(roleName);
        var actorRole = ActorRole.FromName(roleName);

        if (actorRole == ActorRole.MeteredDataResponsible
            || actorRole == ActorRole.EnergySupplier
            || actorRole == ActorRole.BalanceResponsibleParty)
        {
            return actorRole.Code;
        }

        if (WorkaroundFlags.GridOperatorToMeteredDataResponsibleHack && actorRole == ActorRole.GridAccessProvider)
        {
            return ActorRole.MeteredDataResponsible.Code;
        }

        throw new ArgumentException($"Market Role: {actorRole}, is not allowed to request aggregated measure data.");
    }
}
