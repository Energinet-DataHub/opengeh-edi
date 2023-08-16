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
using Google.Protobuf;

namespace Communication.Internal;

public class ServiceBusQueueMessageFactory : IServiceBusQueueMessageFactory
{
    public ServiceBusMessage Create(PointToPointEvent pointToPointEvent)
    {
        ArgumentNullException.ThrowIfNull(pointToPointEvent);

        var serviceBusMessage = new ServiceBusMessage
        {
            Body = new BinaryData(pointToPointEvent.Message.ToByteArray()),
            Subject = pointToPointEvent.EventName,
            MessageId = pointToPointEvent.EventIdentification.ToString(),
        };

        serviceBusMessage.ApplicationProperties.Add("EventMinorVersion", pointToPointEvent.EventMinorVersion);
        // TODO AJW serviceBusMessage.ApplicationProperties.Add("ReferenceId", process.ProcessId.Id.ToString());
        return serviceBusMessage;
    }
}
