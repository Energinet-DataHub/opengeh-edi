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
using Azure.Messaging.ServiceBus;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox.Common
{
    public static class ServiceBusMessageExtension
    {
        private const string MessageTypeName = "MessageType";
        private const string MessageVersionName = "MessageVersion";
        private const string TimeStampName = "OperationTimestamp";
        private const string CorrelationIdName = "OperationCorrelationId";
        private const string EventIdentifierName = "EventIdentification";

        public static void EnrichMetadata(this ServiceBusMessage message, string messageType, int version)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            message.ApplicationProperties.Add(MessageVersionName, version);
            message.ApplicationProperties.Add(MessageTypeName, messageType);
        }

        public static void SetMetadata(this ServiceBusMessage message, Instant timeStamp, string correlationId, Guid eventId)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            message.ApplicationProperties.Add(TimeStampName, timeStamp.ToString());
            message.ApplicationProperties.Add(CorrelationIdName, correlationId);
            message.ApplicationProperties.Add(EventIdentifierName, eventId.ToString());
        }
    }
}
