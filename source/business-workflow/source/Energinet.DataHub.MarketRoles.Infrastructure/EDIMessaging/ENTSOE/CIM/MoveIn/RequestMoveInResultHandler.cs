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
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.ENTSOE.CIM.MoveIn
{
    public class RequestMoveInResultHandler : IBusinessProcessResultHandler<RequestMoveIn>
    {
        private readonly IOutbox _outbox;
        private readonly IOutboxMessageFactory _outboxMessageFactory;

        public RequestMoveInResultHandler(IOutbox outbox, IOutboxMessageFactory outboxMessageFactory)
        {
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _outboxMessageFactory = outboxMessageFactory ?? throw new ArgumentNullException(nameof(outboxMessageFactory));
        }

        public Task HandleAsync(RequestMoveIn request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));
            return result.Success ? CreateAcceptResponseAsync(request, result) : CreateRejectResponseAsync(request, result);
        }

        private Task CreateRejectResponseAsync(RequestMoveIn request, BusinessProcessResult result)
        {
            var ediMessage = new MoveInRequestRejected(result.TransactionId, request.AccountingPointGsrnNumber);
            AddToOutbox(ediMessage);

            return Task.CompletedTask;
        }

        private Task CreateAcceptResponseAsync(RequestMoveIn request, BusinessProcessResult result)
        {
            var ediMessage = new MoveInRequestAccepted(result.TransactionId, request.AccountingPointGsrnNumber);
            AddToOutbox(ediMessage);

            return Task.CompletedTask;
        }

        private void AddToOutbox<TEdiMessage>(TEdiMessage ediMessage)
        {
            var outboxMessage = _outboxMessageFactory.CreateFrom(ediMessage, OutboxMessageCategory.ActorMessage);
            _outbox.Add(outboxMessage);
        }
    }
}
