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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace IncomingMessages.Infrastructure;

public class IncomingRequestAggregatedMeasuredDataSender
{
    private readonly ISerializer _serializer;
    private readonly IServiceBusSenderAdapter _senderCreator;

    public IncomingRequestAggregatedMeasuredDataSender(
        IServiceBusSenderFactory serviceBusSenderFactory,
        IncomingMessagesServiceBusClientConfiguration incomingMessagesServiceBusClientConfiguration,
        ISerializer serializer)
    {
        if (serviceBusSenderFactory == null) throw new ArgumentNullException(nameof(serviceBusSenderFactory));
        if (incomingMessagesServiceBusClientConfiguration == null) throw new ArgumentNullException(nameof(incomingMessagesServiceBusClientConfiguration));
        _serializer = serializer;

        _senderCreator = serviceBusSenderFactory.GetSender(incomingMessagesServiceBusClientConfiguration.QueueName);
    }

    public async Task SendAsync(
        RequestAggregatedMeasureDataDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var serviceBusMessage =
            new ServiceBusMessage(
                _serializer.Serialize(request))
            {
                Subject = nameof(RequestAggregatedMeasureDataDto),
            };

        await _senderCreator.SendAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
    }
}
