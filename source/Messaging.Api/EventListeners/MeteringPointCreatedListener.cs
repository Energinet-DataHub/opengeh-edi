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

using System;
using System.Threading.Tasks;
using Energinet.DataHub.MeteringPoints.IntegrationEvents.CreateMeteringPoint;
using Messaging.Infrastructure.MarketEvaluationPoints;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.EventListeners;

public class MeteringPointCreatedListener
{
    private readonly ILogger<MeteringPointCreatedListener> _logger;
    private readonly MarketEvaluationPointReadModelHandler _readModelHandler;

    public MeteringPointCreatedListener(ILogger<MeteringPointCreatedListener> logger, MarketEvaluationPointReadModelHandler readModelHandler)
    {
        _logger = logger;
        _readModelHandler = readModelHandler;
    }

    [Function("MeteringPointCreatedListener")]
    public Task RunAsync(
        [ServiceBusTrigger("%INTEGRATION_EVENT_TOPIC_NAME%", "%METERING_POINT_CREATED_EVENT_B2B_SUBSCRIPTION_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_INTEGRATION_EVENTS_LISTENER")] byte[] data,
        FunctionContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (data == null) throw new ArgumentNullException(nameof(data));
        _logger.LogInformation($"Received MeteringPointCreated integration event in B2B");

        var meteringPointCreated = MeteringPointCreated.Parser.ParseFrom(data);
        _logger.LogInformation($"Received metering point created event: {meteringPointCreated}");
        return _readModelHandler.WhenAsync(meteringPointCreated);
    }
}
