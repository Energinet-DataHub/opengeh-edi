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
using System.Globalization;
using System.Linq;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.ChangeOfSupplier
{
    public class RequestChangeOfSupplierResultHandler : BusinessProcessResultHandler<RequestChangeOfSupplier>
    {
        public RequestChangeOfSupplierResultHandler(IOutbox outbox, IOutboxMessageFactory outboxMessageFactory)
            : base(outbox, outboxMessageFactory)
        {
        }

        protected override object CreateRejectMessage(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            return new RequestChangeOfSupplierRejected(
                MessageId: Guid.NewGuid().ToString(),
                TransactionId: result.TransactionId,
                MeteringPoint: request.MeteringPointId,
                ReasonCodes: result.ValidationErrors.Select(e => e.GetType().Name).AsEnumerable());
        }

        protected override object CreateAcceptMessage(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var startDate = Instant.FromDateTimeOffset(DateTimeOffset.Parse(request.StartDate, CultureInfo.InvariantCulture));

            return new RequestChangeOfSupplierApproved(
                MessageId: Guid.NewGuid().ToString(),
                TransactionId: result.TransactionId,
                MeteringPointId: request.MeteringPointId,
                RequestingEnergySupplierId: request.EnergySupplierId,
                StartDate: startDate);
        }
    }
}
