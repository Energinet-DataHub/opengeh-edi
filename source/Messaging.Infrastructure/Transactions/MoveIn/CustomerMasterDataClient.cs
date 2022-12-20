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
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EnergySupplying.RequestResponse.Requests;
using Google.Protobuf;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.MessageBus;

namespace Messaging.Infrastructure.Transactions.MoveIn;

public class CustomerMasterDataClient : ICustomerMasterDataClient
{
    private readonly EnergySupplyingServiceBusClientConfiguration _configuration;
    private readonly IServiceBusSenderFactory _clientSenderFactory;

    public CustomerMasterDataClient(
        EnergySupplyingServiceBusClientConfiguration configuration,
        IServiceBusSenderFactory clientSenderFactory)
    {
        _configuration = configuration;
        _clientSenderFactory = clientSenderFactory;
    }

    public Task RequestAsync(FetchCustomerMasterData fetchCustomerMasterData)
    {
        if (fetchCustomerMasterData == null) throw new ArgumentNullException(nameof(fetchCustomerMasterData));
        var message = CreateFrom(fetchCustomerMasterData);
        return _clientSenderFactory
            .GetSender(_configuration.QueueName)
            .SendAsync(message);
    }

    private static ServiceBusMessage CreateFrom(FetchCustomerMasterData fetchMeteringPointMasterData)
    {
        var message = new CustomerMasterDataRequest()
        {
            Processid = fetchMeteringPointMasterData.BusinessProcessId,
        };
        var bytes = message.ToByteArray();
        ServiceBusMessage serviceBusMessage = new(bytes)
        {
            ContentType = "application/octet-stream;charset=utf-8",
        };

        serviceBusMessage.CorrelationId = fetchMeteringPointMasterData.TransactionId;

        return serviceBusMessage;
    }
}
