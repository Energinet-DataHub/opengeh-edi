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
using System.Text.Json;
using Asp.Versioning;
using Energinet.DataHub.Core.App.Common.Users;
using Energinet.DataHub.EDI.B2CWebApi.Factories.V1;
using Energinet.DataHub.EDI.B2CWebApi.Models.V1;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class SendMeasurementsController(
    UserContext<FrontendUser> userContext,
    SendMeasurementsDtoFactory sendMeasurementsDtoFactory,
    IIncomingMessageClient incomingMessageClient) : ControllerBase
{
    public const string RequiredRole = "send-measurements:update"; // TODO #1670: Update with correct role?

    private readonly UserContext<FrontendUser> _userContext = userContext;
    private readonly SendMeasurementsDtoFactory _sendMeasurementsDtoFactory = sendMeasurementsDtoFactory;
    private readonly IIncomingMessageClient _incomingMessageClient = incomingMessageClient;

    [ApiVersion("1.0")]
    [HttpPost]
    [Authorize(Roles = RequiredRole)] // TODO #1670: Update with correct role?
    public async Task<ActionResult> RequestAsync(
        SendMeasurementsRequestV1 request,
        CancellationToken cancellationToken)
    {
        var currentUser = _userContext.CurrentUser;
        var sendMeasurementsDto = _sendMeasurementsDtoFactory.CreateDto(
            Actor.From(currentUser.ActorNumber, currentUser.MarketRole),
            request);

        var incomingMessageStream = await CreateJsonStreamFromStringAsync(sendMeasurementsDto).ConfigureAwait(false);
        var responseMessage = await _incomingMessageClient.ReceiveIncomingMarketMessageAsync(
                incomingMessageStream,
                DocumentFormat.Json,
                IncomingDocumentType.B2CSendMeasurements,
                DocumentFormat.Json,
                cancellationToken)
            .ConfigureAwait(false);

        return responseMessage.IsErrorResponse
            ? BadRequest(responseMessage.MessageBody)
            : Ok(responseMessage.MessageBody);
    }

    private static async Task<IncomingMarketMessageStream> CreateJsonStreamFromStringAsync(SendMeasurementsDto dto)
    {
        var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream, dto).ConfigureAwait(false);

        return new IncomingMarketMessageStream(memoryStream);
    }
}
