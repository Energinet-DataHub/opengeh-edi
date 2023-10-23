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
using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.B2B.RSM.Extensions;

namespace Energinet.DataHub.B2B.RSM.RSM027.V3.Request
{
    public partial class DK_RequestUpdateMasterDataConsumerType : RsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadMasterDataMeteringPointPartyEvent.Length; }
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadMasterDataMeteringPointPartyEvent.ToList();
            payloads.Add((MasterDataMeteringPointPartyEvent)payload);
            PayloadMasterDataMeteringPointPartyEvent = payloads.ToArray();
        }

        public override string GetBusinessReasonCode()
        {
            return ProcessEnergyContext.EnergyBusinessProcess.Value;
        }

        public override DateTime GetCreation()
        {
            return HeaderEnergyDocument.Creation;
        }

        public override string GetDocumentType()
        {
            return "RequestUpdateMasterDataConsumer";
        }

        public override string GetMessageId()
        {
            return HeaderEnergyDocument.Identification;
        }

        public override DateTime? GetOccurrence(int payloadIndex)
        {
            return PayloadMasterDataMeteringPointPartyEvent[payloadIndex].Occurrence;
        }

        public override object GetPayload(int payloadIndex)
        {
            return PayloadMasterDataMeteringPointPartyEvent[payloadIndex];
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return PayloadMasterDataMeteringPointPartyEvent[payloadIndex].Identification;
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
            return "RSM027";
        }

        public override void SetPayloads(List<object> payloads)
        {
            PayloadMasterDataMeteringPointPartyEvent = payloads.Cast<MasterDataMeteringPointPartyEvent>().ToArray();
        }
    }
}
