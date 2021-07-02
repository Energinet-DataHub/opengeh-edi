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
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.MoveIn
{
    public sealed class RequestMoveInResultHandler : BusinessProcessResultPostOfficeCimHandler<RequestMoveIn>
    {
        public RequestMoveInResultHandler(IOutbox outbox, IOutboxMessageFactory outboxMessageFactory)
        : base(outbox, outboxMessageFactory)
        {
        }

        protected override object CreateRejectMessage(RequestMoveIn request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var document = new RejectRequestChangeAccountingPointCharacteristics_MarketDocument
            {
                mRID = request.AccountingPointGsrnNumber,
                Reason = new RejectRequestChangeAccountingPointCharacteristics_MarketDocumentReason
                {
                    code = "A53",
                },
            };
            var cimDocument = Serialize(document);

            return CreatePostOfficeEnvelope(
                request.EnergySupplierGlnNumber,
                cimDocument,
                "RejectRequestChangeAccountingPointCharacteristics");
        }

        protected override object CreateAcceptMessage(RequestMoveIn request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var document = new ConfirmRequestChangeAccountingPointCharacteristics_MarketDocument
            {
                mRID = request.AccountingPointGsrnNumber,
                reasoncode = "A01",
                MktActivityRecord =
                    new ConfirmRequestChangeAccountingPointCharacteristics_MarketDocumentMktActivityRecord
                    {
                        mRID = "String",
                    },
            };
            var cimDocument = Serialize(document);

            return CreatePostOfficeEnvelope(
                request.EnergySupplierGlnNumber,
                cimDocument,
                "ConfirmRequestChangeAccountingPointCharacteristics");
        }

        private static string Serialize<TObject>(TObject toSerialize)
        {
            return Serialize(toSerialize, "urn:ebix:org:ChangeAccountingPointCharacteristics:0:1");
        }
    }
}
