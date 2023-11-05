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
using SoapTest.RSM.Extensions;

namespace SoapTest.RSM.RSM020.V3.Request
{
    public partial class DK_RequestServicesType : RsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadMPServiceEvent.Length; }
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadMPServiceEvent.ToList();
            payloads.Add((MPServiceEvent)payload);
            PayloadMPServiceEvent = payloads.ToArray();
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
            return "RequestServices";
        }

        public override string GetMessageId()
        {
            return HeaderEnergyDocument.Identification;
        }

        public override string GetMeteringPointId(int payloadIndex)
        {
            return PayloadMPServiceEvent[payloadIndex].MeteringPointDomainLocation.Identification.Value;
        }

        public override DateTime? GetOccurrence(int payloadIndex)
        {
            return PayloadMPServiceEvent[payloadIndex].StartOfOccurrence;
        }

        public override string GetOriginalBusinessDocument(int payloadIndex)
        {
            return PayloadMPServiceEvent[payloadIndex].Identification;
        }

        public override object GetPayload(int payloadIndex)
        {
            return PayloadMPServiceEvent[payloadIndex];
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return PayloadMPServiceEvent[payloadIndex].Identification;
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
            return "RSM020";
        }

        public override string GetServiceRequest(int payloadIndex)
        {
            return PayloadMPServiceEvent[payloadIndex].ServiceRequest.Value;
        }

        public override void SetPayloads(Collection<object> payloads)
        {
            PayloadMPServiceEvent = payloads.Cast<MPServiceEvent>().ToArray();
        }
    }
}
