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
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.EnergySupplying.RequestResponse.Requests;
using Messaging.Api.Configuration.Middleware;
using Messaging.Application.MasterData;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;

namespace Messaging.Api.MasterDataReceivers;

public class CustomerMasterDataResponseListener
{
    private readonly ILogger<CustomerMasterDataResponseListener> _logger;
    private readonly CommandSchedulerFacade _commandSchedulerFacade;

    public CustomerMasterDataResponseListener(CommandSchedulerFacade commandSchedulerFacade, ILogger<CustomerMasterDataResponseListener> logger)
    {
        _commandSchedulerFacade = commandSchedulerFacade;
        _logger = logger;
    }

    [Function("CustomerMasterDataResponseListener")]
    public async Task RunAsync([ServiceBusTrigger("%CUSTOMER_MASTER_DATA_RESPONSE_QUEUE_NAME%", Connection = "INTERNAL_SERVICE_BUS_LISTENER_CONNECTION_STRING")] byte[] data, FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var correlationId = context.ParseCorrelationIdFromMessage();
        var response = CustomerMasterDataResponse.Parser.ParseFrom(data);
        if (!string.IsNullOrEmpty(response.Error))
        {
            throw new InvalidOperationException($"Customer master data request failed: {response.Error}");
        }

        var masterDataContent = GetMasterDataContent(response);

        var forwardedCustomerMasterData = new ForwardCustomerMasterData(correlationId, masterDataContent);

        await _commandSchedulerFacade.EnqueueAsync(forwardedCustomerMasterData).ConfigureAwait(false);
        _logger.LogInformation($"Master data response received: {data}");
    }

    private static CustomerMasterDataContent GetMasterDataContent(CustomerMasterDataResponse response)
    {
        var masterData = response.MasterData;
        return new CustomerMasterDataContent(
            string.Empty,
            false,
            masterData.ElectricalHeatingEffectiveDate.ToDateTime().ToUniversalTime().ToInstant(),
            masterData.CustomerId,
            masterData.CustomerName,
            string.Empty,
            string.Empty,
            false,
            false,
            DateTime.Now.ToInstant(),
            new List<UsagePointLocation>());
    }
}
