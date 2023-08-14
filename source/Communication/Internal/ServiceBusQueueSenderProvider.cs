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

using Azure.Messaging.ServiceBus;

namespace Communication.Internal;

public class ServiceBusQueueSenderProvider : IServiceBusQueueSenderProvider
{
    private readonly string _serviceBusPointToPointEventWriteConnectionString;
    private readonly string _topicName;
    private ServiceBusSender? _serviceBusSender;

    public ServiceBusQueueSenderProvider(string serviceBusPointToPointEventWriteConnectionString, string topicName)
    {
        _serviceBusPointToPointEventWriteConnectionString = serviceBusPointToPointEventWriteConnectionString;
        _topicName = topicName;
    }

    public ServiceBusSender Instance
    {
        get
        {
            if (_serviceBusSender == null)
            {
                var serviceBusClient = new ServiceBusClient(_serviceBusPointToPointEventWriteConnectionString);
                _serviceBusSender = serviceBusClient.CreateSender(_topicName);
            }

            return _serviceBusSender;
        }
    }
}
