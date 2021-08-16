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
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier
{
    public class RequestChangeOfSupplierResultHandler : IBusinessProcessResultHandler<RequestChangeOfSupplier>
    {
        private readonly ErrorMessageFactory _errorMessageFactory;
        private readonly IOutbox _outbox;
        private readonly IOutboxMessageFactory _outboxMessageFactory;
        private readonly IJsonSerializer _jsonSerializer;

        public RequestChangeOfSupplierResultHandler(
            ErrorMessageFactory errorMessageFactory,
            IOutbox outbox,
            IOutboxMessageFactory outboxMessageFactory,
            IJsonSerializer jsonSerializer)
        {
            _errorMessageFactory = errorMessageFactory;
            _outbox = outbox;
            _outboxMessageFactory = outboxMessageFactory;
            _jsonSerializer = jsonSerializer;
        }

        public Task HandleAsync(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            return result.Success
                ? CreateAcceptResponseAsync(request, result)
                : CreateRejectResponseAsync(request, result);
        }

        private Task CreateAcceptResponseAsync(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            var startDate = Instant.FromDateTimeOffset(DateTimeOffset.Parse(request.StartDate, CultureInfo.InvariantCulture));

            var ediMessage = new RequestChangeOfSupplierApproved(
                TransactionId: result.TransactionId,
                MessageId: Guid.NewGuid().ToString(),
                AccountingPointId: request.AccountingPointGsrnNumber,
                RequestingEnergySupplierGln: request.EnergySupplierGlnNumber,
                StartDate: startDate);

            var envelope = new PostOfficeEnvelope(string.Empty, string.Empty, _jsonSerializer.Serialize(ediMessage), nameof(RequestChangeOfSupplierApproved), string.Empty); // TODO: add correlation when Telemetry is added

            AddToOutbox(envelope);

            return Task.CompletedTask;
        }

        private Task CreateRejectResponseAsync(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            var ediMessage = new RequestChangeOfSupplierRejected(
                MessageId: Guid.NewGuid().ToString(),
                TransactionId: result.TransactionId,
                MeteringPoint: request.AccountingPointGsrnNumber,
                ReasonCodes: result.ValidationErrors.Select(e => e.GetType().Name).AsEnumerable());

            var envelope = new PostOfficeEnvelope(string.Empty, string.Empty, _jsonSerializer.Serialize(ediMessage), nameof(RequestChangeOfSupplierRejected), string.Empty); // TODO: add correlation when Telemetry is added

            AddToOutbox(envelope);

            return Task.CompletedTask;
        }

        private void AddToOutbox<TEdiMessage>(TEdiMessage ediMessage)
        {
            var outboxMessage = _outboxMessageFactory.CreateFrom(ediMessage, OutboxMessageCategory.ActorMessage);
            _outbox.Add(outboxMessage);
        }
    }
}
