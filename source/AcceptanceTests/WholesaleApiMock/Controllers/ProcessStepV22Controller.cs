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
public class ProcessStepV22Controller : ControllerBase
{
    [HttpPost]
    [ApiVersion("2.2")]
    public async Task<IActionResult> ProcessStepResultAsync([FromBody] ProcessStepResultRequestDtoV2 processStepResultRequestDtoV2)
    {
        ArgumentNullException.ThrowIfNull(processStepResultRequestDtoV2);
        var resultDto = await ProcessStepApplicationServiceV22.GetResultAsync(
            processStepResultRequestDtoV2.BatchId,
            processStepResultRequestDtoV2.GridAreaCode,
            processStepResultRequestDtoV2.TimeSeriesType,
            processStepResultRequestDtoV2.Gln).ConfigureAwait(false);

        return Ok(resultDto);
    }
}

internal class ProcessStepApplicationServiceV22
{
    public static async Task<ProcessStepResultDto> GetResultAsync(Guid batchId, string gridAreaCode, TimeSeriesType timeSeriesType, string gln)
    {
        return await Task.FromResult(
            new ProcessStepResultDto(
                ProcessStepMeteringPointType.Production,
                decimal.One,
                decimal.MinValue,
                decimal.MaxValue,
                new[]
                {
                    new TimeSeriesPointDto(DateTimeOffset.UtcNow, decimal.One, "measured"),
                }))
            .ConfigureAwait(false);
    }
}
