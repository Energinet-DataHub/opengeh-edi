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

namespace SoapTest.RSM.RSM016.V3.Request
{
    public partial class DK_RequestMeteredDataAggregatedType : RsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadMeasuredDataRequest.Length; }
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadMeasuredDataRequest.ToList();
            payloads.Add((MeasuredDataRequest)payload);
            PayloadMeasuredDataRequest = payloads.ToArray();
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
            return "RequestMeteredDataAggregated";
        }

        public override string GetMessageId()
        {
            return HeaderEnergyDocument.Identification;
        }

        public override DateTime? GetOccurrence(int payloadIndex)
        {
            return PayloadMeasuredDataRequest[payloadIndex].ObservationTimeSeriesPeriod.Start;
        }

        public override object GetPayload(int payloadIndex)
        {
            return PayloadMeasuredDataRequest[payloadIndex];
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return PayloadMeasuredDataRequest[payloadIndex].Identification;
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
            return "RSM016";
        }

        public override void SetPayloads(Collection<object> payloads)
        {
            PayloadMeasuredDataRequest = payloads.Cast<MeasuredDataRequest>().ToArray();
        }
    }
}
