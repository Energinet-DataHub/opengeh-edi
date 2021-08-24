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
using Energinet.DataHub.MarketRoles.Application.Common.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;
using Google.Protobuf;
using MediatR;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox.IntegrationEventDispatchers
{
    public abstract class IntegrationEventDispatcher<TTopic, TEvent> : IRequestHandler<TEvent>
        where TTopic : Topic
        where TEvent : IOutboundMessage, IRequest<Unit>
    {
        private readonly ITopicSender<TTopic> _topicSender;
        private readonly ProtobufOutboundMapper<TEvent> _mapper;

        protected IntegrationEventDispatcher(ITopicSender<TTopic> topicSender, ProtobufOutboundMapper<TEvent> mapper)
        {
            _topicSender = topicSender;
            _mapper = mapper;
        }

        public async Task<Unit> Handle(TEvent request, CancellationToken cancellationToken)
        {
            await DispatchMessageAsync(request).ConfigureAwait(false);
            return Unit.Value;
        }

        private async Task DispatchMessageAsync(IOutboundMessage request)
        {
            var message = _mapper.Convert(request);
            var bytes = message.ToByteArray();
            await _topicSender.SendMessageAsync(bytes).ConfigureAwait(false);
        }
    }
}
