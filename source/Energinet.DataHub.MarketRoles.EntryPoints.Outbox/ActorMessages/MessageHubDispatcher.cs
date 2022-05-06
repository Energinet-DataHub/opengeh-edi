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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Messaging;
using MediatR;
using Processing.Infrastructure.EDI;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox.ActorMessages
{
    public class MessageHubDispatcher : IRequestHandler<MessageHubEnvelope>
    {
        private readonly ILocalMessageHubDataAvailableClient _localMessageHubDataAvailableClient;

        public MessageHubDispatcher(ILocalMessageHubDataAvailableClient localMessageHubDataAvailableClient)
        {
            _localMessageHubDataAvailableClient = localMessageHubDataAvailableClient;
        }

        public Task<Unit> Handle(MessageHubEnvelope request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            _localMessageHubDataAvailableClient.DataAvailable(request);

            return Task.FromResult(Unit.Value);
        }
    }
}
