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

using System.Net;
using Energinet.DataHub.EDI.B2CWebApi.Clients;
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class HelloWorldController : ControllerBase
{
    private readonly RequestAggregatedMeasureDataHttpClient _requestAggregatedMeasureDataHttpClient;

    public HelloWorldController(RequestAggregatedMeasureDataHttpClient requestAggregatedMeasureDataHttpClient)
    {
        _requestAggregatedMeasureDataHttpClient = requestAggregatedMeasureDataHttpClient;
    }

    [HttpPost]
    public async Task<ActionResult> RequestAsync(CancellationToken cancellationToken)
    {
        var token = TokenBuilder.BuildToken("unknown", new[] { "unknown" }, "unknown");
        var response = await _requestAggregatedMeasureDataHttpClient.RequestAsync(RequestAggregatedMeasureDataHttpFactory.Create(), token, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
            return Ok("Hello World!");

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return BadRequest(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

        return StatusCode((int)response.StatusCode);
    }
}
