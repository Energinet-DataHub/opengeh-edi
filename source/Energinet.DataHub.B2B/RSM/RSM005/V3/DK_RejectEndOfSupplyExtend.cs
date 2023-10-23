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

namespace Energinet.DataHub.B2B.RSM.RSM005.V3.Reject
{
    public partial class DK_RejectEndOfSupplyType : RsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadResponseEvent.Length; }
        }

        public override string GetMessageId()
        {
            return HeaderEnergyDocument.Identification;
        }

        public override string GetDocumentType()
        {
            return "RejectEndOfSupply";
        }

        public override DateTime GetCreation()
        {
            return HeaderEnergyDocument.Creation;
        }

        public override string GetBusinessReasonCode()
        {
            return ProcessEnergyContext.EnergyBusinessProcess.Value;
        }

        public override string GetRsmNumber()
        {
            return "RSM005";
        }

        public override string GetReferenceMessageId(int payloadIndex)
        {
            return string.Empty;
        }

        public override string GetResponseCode(int payloadIndex)
        {
            return string.Empty;
        }

        public override object GetPayload(int payloadIndex)
        {
            return PayloadResponseEvent[payloadIndex];
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return PayloadResponseEvent[payloadIndex].Identification;
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadResponseEvent.ToList();
            payloads.Add((ResponseEvent)payload);
            PayloadResponseEvent = payloads.ToArray();
        }

        public override void SetPayloads(List<object> payloads)
        {
            PayloadResponseEvent = payloads.Cast<ResponseEvent>().ToArray();
        }
    }
}
