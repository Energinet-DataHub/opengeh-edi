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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.ENTSOE.CIM.ChangeOfSupplier
{
    public class RequestChangeOfSupplierResponder : IBusinessProcessResponder<RequestChangeOfSupplier>
    {
        private readonly IOutbox _outbox;
        private readonly IOutboxMessageFactory _outboxMessageFactory;

        public RequestChangeOfSupplierResponder(IOutbox outbox, IOutboxMessageFactory outboxMessageFactory)
        {
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _outboxMessageFactory = outboxMessageFactory ?? throw new ArgumentNullException(nameof(outboxMessageFactory));
        }

        public Task RespondAsync(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));
            return result.Success ? CreateAcceptResponseAsync(request, result) : CreateRejectResponseAsync(request, result);
        }

        private Task CreateRejectResponseAsync(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            var ediMessage = new RequestChangeOfSupplierRejected(
                MessageId: Guid.NewGuid().ToString(),
                TransactionId: result.TransactionId,
                MeteringPoint: request.MeteringPointId,
                ReasonCodes: result.ValidationErrors.Select(e => e.GetType().Name).AsEnumerable());

            AddToOutbox(ediMessage);

            return Task.CompletedTask;
        }

        private Task CreateAcceptResponseAsync(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            var ediMessage = new RequestChangeOfSupplierApproved(
                MessageId: Guid.NewGuid().ToString(),
                TransactionId: result.TransactionId,
                MeteringPointId: request.MeteringPointId,
                RequestingEnergySupplierId: request.EnergySupplierId,
                StartDate: request.StartDate);

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
