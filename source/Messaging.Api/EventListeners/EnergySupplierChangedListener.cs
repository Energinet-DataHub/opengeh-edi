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
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.Infrastructure.MarketEvaluationPoints;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.EventListeners;

public class EnergySupplierChangedListener
{
    private readonly ILogger<EnergySupplierChangedListener> _logger;
    private readonly MarketEvaluationPointReadModelHandler _readModelHandler;

    public EnergySupplierChangedListener(ILogger<EnergySupplierChangedListener> logger, MarketEvaluationPointReadModelHandler readModelHandler)
    {
        _logger = logger;
        _readModelHandler = readModelHandler;
    }

    [Function("EnergySupplierChangedListener")]
    public Task RunAsync(
        [ServiceBusTrigger("%INTEGRATION_EVENT_TOPIC_NAME%", "%ENERGY_SUPPLIER_CHANGED_EVENT_SUBSCRIPTION_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] data,
        FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var energySupplierChanged = Energinet.DataHub.EnergySupplying.IntegrationEvents.EnergySupplierChanged.Parser.ParseFrom(data);
        _logger.LogInformation($"Received EnergySupplierChanged integration event: {energySupplierChanged}");
        return _readModelHandler.WhenAsync(energySupplierChanged);
    }
}
