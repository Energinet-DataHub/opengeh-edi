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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration;
using Application.WholeSale;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.JsonSerialization;
using Infrastructure.Configuration.MessageBus;
using Infrastructure.Configuration.Serialization;

namespace Infrastructure.WholeSale;

public class WholeSaleInbox<T> : IWholeSaleInBox<T>
{
    private const string CorrelationId = "CorrelationID";
    private readonly ISerializer _jsonSerializer;
    private readonly ICorrelationContext _correlationContext;
    private readonly IServiceBusSenderAdapter _senderCreator;

    public WholeSaleInbox(
        ISerializer jsonSerializer,
        ICorrelationContext correlationContext,
        IServiceBusSenderFactory serviceBusSenderFactory,
        WholeSaleServiceBusClientConfiguration wholeSaleServiceBusClientConfiguration)
    {
        if (serviceBusSenderFactory == null) throw new ArgumentNullException(nameof(serviceBusSenderFactory));
        if (wholeSaleServiceBusClientConfiguration == null) throw new ArgumentNullException(nameof(wholeSaleServiceBusClientConfiguration));

        _jsonSerializer = jsonSerializer;
        _correlationContext = correlationContext;
        _senderCreator = serviceBusSenderFactory.GetSender(wholeSaleServiceBusClientConfiguration.QueueName);
    }

    public async Task SendAsync(T request, CancellationToken cancellationToken)
    {
        await _senderCreator.SendAsync(CreateMessage(request), cancellationToken).ConfigureAwait(false);
    }

    private ServiceBusMessage CreateMessage(T request)
    {
        var json = _jsonSerializer.Serialize(request);
        var data = Encoding.UTF8.GetBytes(json);
        var message = new ServiceBusMessage(data);
        message.ApplicationProperties.Add(CorrelationId, _correlationContext.Id);
        return message;
    }
}
