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
using Azure.Messaging.ServiceBus;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;

namespace WholeSaleApiStub.Controllers;

[ApiController]
[Route("api")]
public class SimulateProcessCompletedController : ControllerBase
{
    private readonly ServiceBusSender _serviceBusSender;

    public SimulateProcessCompletedController(ServiceBusSender serviceBusSender)
    {
        _serviceBusSender = serviceBusSender;
    }

    [HttpPost("SimulateProcessCompleted")]
    public async Task<ActionResult> SimulateProcessCompletedAsync(SimulateProcessHasCompleted request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processCompletedEvent = new Energinet.DataHub.Wholesale.Contracts.Events.ProcessCompleted()
        {
            BatchId = Guid.NewGuid().ToString(),
            GridAreaCode = request.GridArea,
            PeriodEndUtc = DateTime.UtcNow.ToTimestamp(),
            PeriodStartUtc = DateTime.UtcNow.ToTimestamp(),
        };
        var serviceBusMessage = new ServiceBusMessage()
        {
            Body = new BinaryData(processCompletedEvent.ToByteArray()),
            ContentType = "application/octet-stream",
            MessageId = Guid.NewGuid().ToString(),
            Subject = "balancefixingcompleted",
        };
        await _serviceBusSender.SendMessageAsync(serviceBusMessage).ConfigureAwait(false);

        return Ok();
    }
}

public record SimulateProcessHasCompleted(string GridArea);
