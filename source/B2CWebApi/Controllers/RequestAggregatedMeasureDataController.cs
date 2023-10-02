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
using Energinet.DataHub.EDI.B2CWebApi.Clients;
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class RequestAggregatedMeasureDataController : ControllerBase
{
    private readonly RequestAggregatedMeasureDataHttpClient _requestAggregatedMeasureDataHttpClient;
    private readonly UserContext<FrontendUser> _userContext;

    public RequestAggregatedMeasureDataController(
        RequestAggregatedMeasureDataHttpClient requestAggregatedMeasureDataHttpClient,
        UserContext<FrontendUser> userContext)
    {
        _requestAggregatedMeasureDataHttpClient = requestAggregatedMeasureDataHttpClient;
        _userContext = userContext;
    }

    [HttpPost]
    public async Task<ActionResult> RequestAsync(RequestAggregatedMeasureDataMarketRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _userContext.CurrentUser;
        var token = GetToken(currentUser);

        var validationMessage = await _requestAggregatedMeasureDataHttpClient.RequestAsync(
            RequestAggregatedMeasureDataHttpFactory.Create(request, currentUser.ActorNumber, currentUser.Role),
            token,
            cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(validationMessage))
            return Ok();

        return BadRequest(validationMessage);
    }

    private static string GetToken(FrontendUser currentUser)
    {
        return TokenBuilder.BuildToken(
            currentUser.ActorNumber,
            new[]
            {
#pragma warning disable CA1308
                currentUser.Role.ToLowerInvariant(),
#pragma warning restore CA1308
            },
            currentUser.Azp);
    }
}
