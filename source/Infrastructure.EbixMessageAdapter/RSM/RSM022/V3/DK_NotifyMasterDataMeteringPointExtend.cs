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

namespace Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.RSM.RSM022.V3.Notify
{
    public partial class DK_NotifyMasterDataMeteringPointType : RsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadMasterDataMPEvent.Length; }
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadMasterDataMPEvent.ToList();
            payloads.Add((MasterDataMPEvent)payload);
            PayloadMasterDataMPEvent = payloads.ToArray();
        }

        public override string GetBusinessReasonCode()
        {
            return processEnergyContextField.EnergyBusinessProcess.Value;
        }

        public override DateTime GetCreation()
        {
            return HeaderEnergyDocument.Creation;
        }

        public override string GetDocumentType()
        {
            return "NotifyMasterDataMeteringPoint";
        }

        public override string GetMessageId()
        {
            return HeaderEnergyDocument.Identification;
        }

        public override string GetMeteringPointId(int payloadIndex)
        {
            return PayloadMasterDataMPEvent[payloadIndex].MeteringPointDomainLocation.Identification.Value;
        }

        public override DateTime? GetOccurrence(int payloadIndex)
        {
            return PayloadMasterDataMPEvent[payloadIndex].Occurrence;
        }

        public override string GetOriginalBusinessDocument(int payloadIndex)
        {
            return PayloadMasterDataMPEvent[payloadIndex].Identification;
        }

        public override object GetPayload(int payloadIndex)
        {
            return PayloadMasterDataMPEvent[payloadIndex];
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return PayloadMasterDataMPEvent[payloadIndex].Identification;
        }

        public override string GetReferenceMessageId(int payloadIndex)
        {
            return string.Empty;
        }

        public override string GetResponseCode(int payloadIndex)
        {
            return string.Empty;
        }

        public override string GetRsmNumber()
        {
            return "RSM022";
        }

        public override void SetPayloads(Collection<object> payloads)
        {
            PayloadMasterDataMPEvent = payloads.Cast<MasterDataMPEvent>().ToArray();
        }
    }
}
