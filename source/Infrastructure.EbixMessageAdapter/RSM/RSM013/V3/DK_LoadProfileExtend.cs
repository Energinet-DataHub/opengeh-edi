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

namespace SoapTest.RSM.RSM013.V3
{
    public partial class DK_LoadProfileType : RsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadEnergyTimeSeries.Length; }
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadEnergyTimeSeries.ToList();
            payloads.Add((EnergyTimeSeries)payload);
            PayloadEnergyTimeSeries = payloads.ToArray();
        }

        public override string GetBusinessReasonCode()
        {
            return string.Empty;
        }

        public override DateTime GetCreation()
        {
            return HeaderEnergyDocument.Creation;
        }

        public override string GetDocumentType()
        {
            return "LoadProfile";
        }

        public override string GetMessageId()
        {
            return HeaderEnergyDocument.Identification;
        }

        public override object GetPayload(int payloadIndex)
        {
            return PayloadEnergyTimeSeries[payloadIndex];
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return PayloadEnergyTimeSeries[payloadIndex].Identification;
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
            return "RSM013";
        }

        public override void SetPayloads(Collection<object> payloads)
        {
            PayloadEnergyTimeSeries = payloads.Cast<EnergyTimeSeries>().ToArray();
        }
    }
}
