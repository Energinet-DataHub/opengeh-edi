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
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.MoveIn
{
    public sealed class RequestMoveInResultHandler : BusinessProcessResultPostOfficeCimHandler<RequestMoveIn>
    {
        private readonly ICorrelationContext _correlationContext;
        private readonly ISystemDateTimeProvider _dateTimeProvider;

        public RequestMoveInResultHandler(
            ICorrelationContext correlationContext,
            ISystemDateTimeProvider dateTimeProvider,
            IOutbox outbox,
            IOutboxMessageFactory outboxMessageFactory)
        : base(outbox, outboxMessageFactory)
        {
            _correlationContext = correlationContext;
            _dateTimeProvider = dateTimeProvider;
        }

        protected override object CreateRejectMessage(RequestMoveIn request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var document = new RejectRequestChangeOfSupplier_MarketDocument
            {
                mRID = Guid.NewGuid().ToString(),
                type = 414,
                processprocessType = "E65",
                sender_MarketParticipantmRID = new()
                {
                    Value = "DataHub GLN", // TODO: use correct GLN
                    codingScheme = "9",
                },
                sender_MarketParticipantmarketRoletype = "EZ",
                receiver_MarketParticipantmRID = new()
                {
                    Value = request.EnergySupplierGlnNumber,
                    codingScheme = "9",
                },
                receiver_MarketParticipantmarketRoletype = "DDQ",
                createdDateTime = _dateTimeProvider.Now().ToDateTimeUtc(),
                Reason = new()
                {
                    code = "41",
                },
                MktActivityRecord = new()
                {
                    mRID = Guid.NewGuid().ToString(),
                    businessProcessReference_MktActivityRecordmRID = _correlationContext.GetCorrelationId(),
                    originalTransactionIDReference_MktActivityRecordmRID = request.TransactionId,
                    marketEvaluationPointmRID = request.AccountingPointGsrnNumber,
                    start_DateAndOrTimedate = request.MoveInDate.ToDateTimeUtc(),
                    Reason = new RejectRequestChangeOfSupplier_MarketDocumentMktActivityRecordReason[]
                    {
                        new()
                        {
                            code = "TODO", // TODO: Use error conversion.
                        },
                    },
                },
            };
            var cimDocument = Serialize(document);

            return CreatePostOfficeEnvelope(
                request.EnergySupplierGlnNumber,
                cimDocument,
                "RejectRequestChangeOfSupplier");
        }

        protected override object CreateAcceptMessage(RequestMoveIn request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var document = new ConfirmRequestChangeOfSupplier_MarketDocument
            {
                mRID = Guid.NewGuid().ToString(),
                type = 414,
                processprocessType = "E65",
                sender_MarketParticipantmRID = new()
                {
                    Value = "DataHub GLN", // TODO: use correct GLN
                    codingScheme = "9",
                },
                sender_MarketParticipantmarketRoletype = "EZ",
                receiver_MarketParticipantmRID = new()
                {
                    Value = request.EnergySupplierGlnNumber,
                    codingScheme = "9",
                },
                receiver_MarketParticipantmarketRoletype = "DDQ",
                createdDateTime = _dateTimeProvider.Now().ToDateTimeUtc(),
                reasoncode = "39",
                MktActivityRecord = new()
                {
                    mRID = Guid.NewGuid().ToString(),
                    businessProcessReference_MktActivityRecordmRID = _correlationContext.GetCorrelationId(),
                    originalTransactionReference_MktActivityRecordmRID = request.TransactionId,
                    marketEvaluationPointmRID = request.AccountingPointGsrnNumber,
                    start_DateAndOrTimedate = request.MoveInDate.ToDateTimeUtc(),
                },
            };
            var cimDocument = Serialize(document);

            return CreatePostOfficeEnvelope(
                request.EnergySupplierGlnNumber,
                cimDocument,
                "ConfirmRequestChangeOfSupplier");
        }

        private static string Serialize<TObject>(TObject toSerialize)
        {
            return Serialize(toSerialize, "urn:ebix:org:ChangeOfSupplier:0:1");
        }
    }
}
