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
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.RSM.Extensions;

namespace Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.RSM.RSM006.V3.Reject
{
    public partial class DK_RejectQueryMasterDataType : ResponseRsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadMeterEvent.Length; }
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadMeterEvent.ToList();
            payloads.Add((MeterEvent)payload);
            PayloadMeterEvent = payloads.ToArray();
        }

        public override string GetBusinessReasonCode()
        {
            return processEnergyContextField.EnergyBusinessProcess.Value;
        }

        public override DateTime GetCreation()
        {
            return headerEnergyDocumentField.Creation;
        }

        public override string GetDocumentType()
        {
            return "RejectQueryMasterData";
        }

        public override string GetMessageId()
        {
            return headerEnergyDocumentField.Identification;
        }

        public override object GetPayload(int payloadIndex)
        {
            return PayloadMeterEvent[payloadIndex];
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return PayloadMeterEvent[payloadIndex].Identification;
        }

        public override string GetReferenceMessageId(int payloadIndex)
        {
            return PayloadMeterEvent[payloadIndex].OriginalBusinessDocument;
        }

        public override string GetResponseCode(int payloadIndex)
        {
            return PayloadMeterEvent[payloadIndex].ResponseReasonType.Value;
        }

        public override string GetRsmNumber()
        {
            return "RSM006";
        }

        public override void SetPayloads(Collection<object> payloads)
        {
            PayloadMeterEvent = payloads.Cast<MeterEvent>().ToArray();
        }
    }
}
