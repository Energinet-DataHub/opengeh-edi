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

using Energinet.DataHub.Wholesale.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AcceptanceTest.WholesaleApiMock.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/ProcessStepResult")]
public class ProcessStepV23Controller : ControllerBase
{
    [HttpPost]
    [ApiVersion("2.3")]
    public async Task<IActionResult> ProcessStepResultAsync([FromBody] ProcessStepActorsRequest processStepActorsRequest)
    {
        ArgumentNullException.ThrowIfNull(processStepActorsRequest);
        var actors = await ProcessStepApplicationServiceV23.GetActorsAsync(
            processStepActorsRequest).ConfigureAwait(false);

        return Ok(actors);
    }
}

internal class ProcessStepApplicationServiceV23
{
    public static async Task<WholesaleActorDto[]> GetActorsAsync(ProcessStepActorsRequest processStepActorsRequest)
    {
        var actors = new List<WholesaleActorDto>()
        {
            new("1234567890123456"),
        };

        return await Task.FromResult(actors.Select(batchActor => new WholesaleActorDto(batchActor.Gln)).ToArray()).ConfigureAwait(false);
    }
}
