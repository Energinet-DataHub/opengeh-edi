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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.Notifications;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Processing
{
    public class IntegrationEventReceiver
    {
        private readonly ILogger _logger;
        private readonly ProtobufInboundMapperFactory _protobufInboundMapperFactory;
        private readonly IProtobufMessageFactory _protobufMessageFactory;
        private readonly INotificationReceiver _notificationReceiver;
        private readonly IJsonSerializer _jsonSerializer;

        public IntegrationEventReceiver(
            ILogger logger,
            ProtobufInboundMapperFactory protobufInboundMapperFactory,
            IProtobufMessageFactory protobufMessageFactory,
            INotificationReceiver notificationReceiver,
            IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _protobufInboundMapperFactory = protobufInboundMapperFactory ?? throw new ArgumentNullException(nameof(protobufInboundMapperFactory));
            _protobufMessageFactory = protobufMessageFactory ?? throw new ArgumentNullException(nameof(protobufMessageFactory));
            _notificationReceiver = notificationReceiver ?? throw new ArgumentNullException(nameof(notificationReceiver));
            _jsonSerializer = jsonSerializer;
        }

        [Function("IntegrationEventReceiver")]
        public Task RunAsync([ServiceBusTrigger("%INTEGRATION_EVENT_QUEUE%", Connection = "INTEGRATION_EVENT_QUEUE_CONNECTION")] byte[] data, FunctionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var eventTypeName = GetEventTypeName(context);
            _logger.LogInformation($"Received notification event of type {eventTypeName}.");

            var message = _protobufMessageFactory.CreateMessageFrom(data, eventTypeName);
            var mapper = _protobufInboundMapperFactory.GetMapper(message.GetType());
            var notification = mapper.Convert(message);

            return _notificationReceiver.PublishAndCommitAsync(notification);
        }

        private string GetEventTypeName(FunctionContext context)
        {
            context.BindingContext.BindingData.TryGetValue("UserProperties", out var metadata);

            if (metadata is null)
            {
                throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
            }

            var eventMetadata = _jsonSerializer.Deserialize<EventMetadata>(metadata.ToString() ?? throw new InvalidOperationException());
            return eventMetadata.EventType ?? throw new InvalidOperationException("Service bus metadata property MessageType is missing");
        }
    }
}
