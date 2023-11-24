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

using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Google.Protobuf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class RequestAggregatedMeasureDataController : ControllerBase
{
    private readonly UserContext<FrontendUser> _userContext;
    private readonly DateTimeZone _dateTimeZone;
    private readonly IIncomingMessageParser _incomingMessageParser;

    public RequestAggregatedMeasureDataController(
        UserContext<FrontendUser> userContext,
        DateTimeZone dateTimeZone,
        IIncomingMessageParser incomingMessageParser)
    {
        _userContext = userContext;
        _dateTimeZone = dateTimeZone;
        _incomingMessageParser = incomingMessageParser;
    }

    [HttpPost]
    [Authorize(Roles = "request-aggregated-measured-data:view")]
    public async Task<ActionResult> RequestAsync(RequestAggregatedMeasureDataMarketRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _userContext.CurrentUser;

        var message =
            RequestAggregatedMeasureDataHttpFactory.Create(
                request,
                currentUser.ActorNumber,
                currentUser.Role,
                _dateTimeZone);

        var responseMessage = await _incomingMessageParser.ParseAsync(
                GenerateStreamFromString(message.ToByteArray()),
                DocumentFormat.Proto,
                IncomingDocumentType.RequestAggregatedMeasureData,
                cancellationToken,
                DocumentFormat.Json)
            .ConfigureAwait(false);

        if (responseMessage.IsErrorResponse)
        {
            return BadRequest();
        }

        return Ok(responseMessage.MessageBody);
    }

    private static Stream GenerateStreamFromString(byte[] dataToStream)
    {
        if (dataToStream == null) throw new ArgumentNullException(nameof(dataToStream));
        var stream = new MemoryStream();
        stream.Write(dataToStream, 0, dataToStream.Length);
        stream.Position = 0;
        return stream;
    }
}
