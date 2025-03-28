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
public class TempRequestWholesaleSettlementController : ControllerBase
{
    private const string WholesaleSettlementMessageType = "D21";
    private const string WholesaleSettlementBusinessType = "23";

    private readonly IClock _clock;
    private readonly UserContext<FrontendUser> _userContext;
    private readonly IIncomingMessageClient _incomingMessageClient;
    private readonly ISerializer _serializer;
    private readonly IAuditLogger _auditLogger;

    public TempRequestWholesaleSettlementController(
        IClock clock,
        UserContext<FrontendUser> userContext,
        IIncomingMessageClient incomingMessageClient,
        ISerializer serializer,
        IAuditLogger auditLogger)
    {
        _clock = clock;
        _userContext = userContext;
        _incomingMessageClient = incomingMessageClient;
        _serializer = serializer;
        _auditLogger = auditLogger;
    }

    [ApiVersion("1.0")]
    [HttpPost]
    [Authorize(Roles = "request-wholesale-settlement:view")]
    public async Task<ActionResult> RequestAsync(
        RequestWholesaleServicesMarketDocumentV2 request,
        CancellationToken cancellationToken)
    {
        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.RequestWholesaleResults,
                activityOrigin: HttpContext.Request.GetDisplayUrl(),
                activityPayload: request,
                affectedEntityType: AuditLogEntityType.RequestWholesaleServices,
                affectedEntityKey: null)
            .ConfigureAwait(false);

        var currentUser = _userContext.CurrentUser;

        var message = new RequestWholesaleSettlementDto(
            currentUser.ActorNumber,
            MapRoleNameToCode(currentUser.MarketRole),
            DataHubDetails.DataHubActorNumber.Value,
            ActorRole.MeteredDataAdministrator.Code,
            request.BusinessReason,
            WholesaleSettlementMessageType,
            Guid.NewGuid().ToString(),
            _clock.GetCurrentInstant().ToString(),
            WholesaleSettlementBusinessType,
            request.Series.Select(s => new RequestWholesaleSettlementSeries(
                s.Id,
                s.StartDateAndOrTimeDateTime,
                s.EndDateAndOrTimeDateTime,
                s.MeteringGridAreaDomainId,
                s.EnergySupplierMarketParticipantId,
                s.SettlementVersion,
                s.Resolution,
                s.ChargeOwner,
                s.ChargeTypes.Select(ct => new RequestWholesaleSettlementChargeType(ct.Id, ct.Type)).ToList().AsReadOnly())).ToList().AsReadOnly());

        var responseMessage = await _incomingMessageClient.ReceiveIncomingMarketMessageAsync(
                GenerateStreamFromString(_serializer.Serialize(message)),
                DocumentFormat.Json,
                IncomingDocumentType.B2CRequestWholesaleSettlement,
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

        if (actorRole == ActorRole.SystemOperator
            || actorRole == ActorRole.EnergySupplier
            || actorRole == ActorRole.GridAccessProvider)
        {
            return actorRole.Code;
        }

        throw new ArgumentException($"Market Role: {actorRole}, is not allowed to request wholesale settlement.");
    }
}
