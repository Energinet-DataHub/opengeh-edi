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
using Energinet.DataHub.Core.App.Common.Users;
using Energinet.DataHub.EDI.AuditLog;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class RequestAggregatedMeasureDataController : ControllerBase
{
    private readonly UserContext<FrontendUser> _userContext;
    private readonly DateTimeZone _dateTimeZone;
    private readonly IIncomingMessageClient _incomingMessageClient;
    private readonly ISerializer _serializer;
    private readonly IClock _clock;
    private readonly IAuditLogger _auditLogger;

    public RequestAggregatedMeasureDataController(
        UserContext<FrontendUser> userContext,
        DateTimeZone dateTimeZone,
        IIncomingMessageClient incomingMessageClient,
        ISerializer serializer,
        IClock clock,
        IAuditLogger auditLogger)
    {
        _userContext = userContext;
        _dateTimeZone = dateTimeZone;
        _incomingMessageClient = incomingMessageClient;
        _serializer = serializer;
        _clock = clock;
        _auditLogger = auditLogger;
    }

    [HttpPost]
    [Authorize(Roles = "request-aggregated-measured-data:view")]
    public async Task<ActionResult> RequestAsync(RequestAggregatedMeasureDataMarketRequest request, CancellationToken cancellationToken)
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

        var message =
            RequestAggregatedMeasureDataDtoFactory.Create(
                request,
                currentUser.ActorNumber,
                currentUser.MarketRole,
                _dateTimeZone,
                _clock.GetCurrentInstant());

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
}
