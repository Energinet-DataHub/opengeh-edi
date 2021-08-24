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
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Acknowledgements;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.MoveIn
{
    public sealed class RequestMoveInResultHandler : BusinessProcessResultHandler<RequestMoveIn>
    {
        private const string XmlNamespace = "urn:ebix:org:ChangeAccountingPointCharacteristics:0:1";

        private readonly ErrorMessageFactory _errorMessageFactory;
        private readonly ICorrelationContext _correlationContext;
        private readonly ISystemDateTimeProvider _dateTimeProvider;

        public RequestMoveInResultHandler(
            ErrorMessageFactory errorMessageFactory,
            ICorrelationContext correlationContext,
            ISystemDateTimeProvider dateTimeProvider,
            IOutbox outbox,
            IOutboxMessageFactory outboxMessageFactory)
        : base(outbox, outboxMessageFactory)
        {
            _errorMessageFactory = errorMessageFactory;
            _correlationContext = correlationContext;
            _dateTimeProvider = dateTimeProvider;
        }

        protected override object CreateRejectMessage(RequestMoveIn request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var errors = result.ValidationErrors
                .Select(error => _errorMessageFactory.GetErrorMessage(error));

            var message = new RejectMessage(
                DocumentName: "RejectRequestChangeOfSupplier_MarketDocument",
                Id: Guid.NewGuid().ToString(),
                Type: "414",
                ProcessType: "E65",
                BusinessSectorType: "E21",
                Sender: new MarketParticipant(
                    Id: "DataHub GLN", // TODO: Use correct GLN
                    CodingScheme: "9",
                    Role: "EZ"),
                Receiver: new MarketParticipant(
                    Id: request.EnergySupplierGlnNumber,
                    CodingScheme: "9",
                    Role: "DDQ"),
                CreatedDateTime: _dateTimeProvider.Now(),
                Reason: new Reason(
                    Code: "41",
                    Text: string.Empty),
                MarketActivityRecord: new MarketActivityRecordWithReasons(
                    Id: Guid.NewGuid().ToString(),
                    BusinessProcessReference: _correlationContext.GetCorrelationId(),
                    MarketEvaluationPoint: request.AccountingPointGsrnNumber,
                    StartDateAndOrTime: request.MoveInDate,
                    OriginalTransaction: request.TransactionId,
                    Reasons: errors.Select(error => new Reason(error.Code, error.Description)).ToList()));

            var document = AcknowledgementXmlSerializer.Serialize(message, XmlNamespace);

            return CreatePostOfficeEnvelope(
                recipient: request.EnergySupplierGlnNumber,
                cimContent: document,
                messageType: "RejectRequestChangeOfSupplier");
        }

        protected override object CreateAcceptMessage(RequestMoveIn request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var message = new ConfirmMessage(
                DocumentName: "ConfirmRequestChangeOfSupplier_MarketDocument",
                Id: Guid.NewGuid().ToString(),
                Type: "414",
                ProcessType: "E65",
                BusinessSectorType: "E21",
                Sender: new MarketParticipant(
                    Id: "DataHub GLN", // TODO: Use correct GLN
                    CodingScheme: "9",
                    Role: "EZ"),
                Receiver: new MarketParticipant(
                    Id: request.EnergySupplierGlnNumber,
                    CodingScheme: "9",
                    Role: "DDQ"),
                CreatedDateTime: _dateTimeProvider.Now(),
                ReasonCode: "39",
                MarketActivityRecord: new MarketActivityRecord(
                    Id: Guid.NewGuid().ToString(),
                    BusinessProcessReference: _correlationContext.GetCorrelationId(),
                    MarketEvaluationPoint: request.AccountingPointGsrnNumber,
                    StartDateAndOrTime: request.MoveInDate,
                    OriginalTransaction: request.TransactionId));

            var document = AcknowledgementXmlSerializer.Serialize(message, XmlNamespace);

            return CreatePostOfficeEnvelope(
                recipient: request.EnergySupplierGlnNumber,
                cimContent: document,
                messageType: "ConfirmRequestChangeOfSupplier");
        }

        private static PostOfficeEnvelope CreatePostOfficeEnvelope(string recipient, string cimContent, string messageType)
        {
            return new(
                Id: Guid.NewGuid().ToString(),
                Recipient: recipient,
                Content: cimContent,
                MessageType: messageType,
                Correlation: string.Empty); // TODO: add correlation when Telemetry is added
        }
    }
}
