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
using Newtonsoft.Json;

namespace Messaging.Infrastructure.Configuration.MessageBus;

public class RemoteBusinessService<TRequest, TReply> : IRemoteBusinessService<TRequest, TReply>
    where TRequest : class
    where TReply : class
{
    private readonly IRemoteBusinessServiceRequestSenderAdapter<TRequest> _requestSender;
    private readonly string _responseQueueName;

    public RemoteBusinessService(IRemoteBusinessServiceRequestSenderAdapter<TRequest> requestSender, string responseQueueName)
    {
        _requestSender = requestSender;
        _responseQueueName = responseQueueName;
    }

    public Task SendRequestAsync(TRequest message, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(message);
        var serviceBusMessage = new ServiceBusMessage(JsonConvert.SerializeObject(message))
        {
            ContentType = "application/json",
            CorrelationId = correlationId,
            ReplyTo = _responseQueueName,
        };

        return _requestSender.SendAsync(serviceBusMessage);
    }
}
