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
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.EntryPoints.Outbox.Common;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEvents.EnergySupplierChange;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEvents.EnergySupplierChangeRegistered;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox.IntegrationEventDispatchers
{
    public class EnergySupplierChangeRegisteredDispatcher : IntegrationEventDispatcher<EnergySupplierChangeRegisteredTopic, EnergySupplierChangeRegisteredIntegrationEvent>
    {
        public EnergySupplierChangeRegisteredDispatcher(
            ITopicSender<EnergySupplierChangeRegisteredTopic> topicSender,
            ProtobufOutboundMapper<EnergySupplierChangeRegisteredIntegrationEvent> mapper,
            IIntegrationEventMessageFactory integrationEventMessageFactory,
            IIntegrationMetadataContext integrationMetadataContext)
            : base(topicSender, mapper, integrationEventMessageFactory, integrationMetadataContext)
        {
        }

        protected override void EnrichMessage(ServiceBusMessage serviceBusMessage)
        {
            serviceBusMessage.EnrichMetadata(
                nameof(EnergySupplierChanged),
                1);
        }
    }
}
