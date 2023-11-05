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

namespace SoapTest.RSM.RSM024.V3.Request
{
    public partial class DK_RequestCancellationType : RsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadMPEvent.Length; }
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadMPEvent.ToList();
            payloads.Add((MPEvent)payload);
            PayloadMPEvent = payloads.ToArray();
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
            return "RequestCancellation";
        }

        public override string GetMessageId()
        {
            return headerEnergyDocumentField.Identification;
        }

        public override object GetPayload(int payloadIndex)
        {
            return PayloadMPEvent[payloadIndex];
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return PayloadMPEvent[payloadIndex].Identification;
        }

        public override string GetReferenceMessageId(int payloadIndex)
        {
            return PayloadMPEvent[payloadIndex].OriginalBusinessDocument;
        }

        public override string GetResponseCode(int payloadIndex)
        {
            return string.Empty;
        }

        public override string GetRsmNumber()
        {
            return "RSM024";
        }

        public override void SetPayloads(Collection<object> payloads)
        {
            PayloadMPEvent = payloads.Cast<MPEvent>().ToArray();
        }
    }
}
