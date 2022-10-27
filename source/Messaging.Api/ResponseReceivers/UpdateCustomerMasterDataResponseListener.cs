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
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.ResponseReceivers;

public class UpdateCustomerMasterDataResponseListener
{
    private readonly ILogger<UpdateCustomerMasterDataResponseListener> _logger;

    public UpdateCustomerMasterDataResponseListener(ILogger<UpdateCustomerMasterDataResponseListener> logger)
    {
        _logger = logger;
    }

    [Function("UpdateCustomerMasterDataResponseListener")]
    public void Run([ServiceBusTrigger("%CUSTOMER_MASTER_DATA_UPDATE_RESPONSE_QUEUE_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] data, FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger.LogInformation($"Received update customer master data response");
    }
}
