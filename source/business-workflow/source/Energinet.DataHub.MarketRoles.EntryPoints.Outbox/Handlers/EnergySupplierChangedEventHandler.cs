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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEventDispatching.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEventDispatching.MoveIn;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEventDispatching.MoveIn.Messages;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;
using Google.Protobuf;
using MediatR;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox.Handlers
{
    public class EnergySupplierChangedEvent : IRequestHandler<EnergySupplierChangedIntegrationEvent>
    {
        private readonly ITopicSender<EnergySupplierChangedTopic> _topicSender;
        private readonly ProtobufOutboundMapper<EnergySupplierChangedIntegrationEvent> _mapper;

        public EnergySupplierChangedEvent(
            ITopicSender<EnergySupplierChangedTopic> topicSender,
            ProtobufOutboundMapper<EnergySupplierChangedIntegrationEvent> mapper)
        {
            _topicSender = topicSender;
            _mapper = mapper;
        }

        public async Task<Unit> Handle(EnergySupplierChangedIntegrationEvent request, CancellationToken cancellationToken)
        {
            var message = _mapper.Convert(request);
            var bytes = message.ToByteArray();
            await _topicSender.SendMessageAsync(bytes).ConfigureAwait(false);
            return Unit.Value;
        }
    }
}
