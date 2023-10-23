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

namespace Energinet.DataHub.B2B.RSM.RSM011.V3.Notify
{
    public partial class DK_NonContinuousMeteringType : RsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadNonContinuousEnergyTimeSeries.Length; }
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadNonContinuousEnergyTimeSeries.ToList();
            payloads.Add((NonContinuousEnergyTimeSeries)payload);
            PayloadNonContinuousEnergyTimeSeries = payloads.ToArray();
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
            return "NonContinuousMetering";
        }

        public override string GetMessageId()
        {
            return HeaderEnergyDocument.Identification;
        }

        public override object GetPayload(int payloadIndex)
        {
            return PayloadNonContinuousEnergyTimeSeries[payloadIndex];
        }

        public override string GetPayloadMessageId(int payloadIndex)
        {
            return PayloadNonContinuousEnergyTimeSeries[payloadIndex].Identification;
        }

        public override string GetReferenceMessageId(int payloadIndex)
        {
            return PayloadNonContinuousEnergyTimeSeries[payloadIndex].OriginalBusinessDocument;
        }

        public override string GetResponseCode(int payloadIndex)
        {
            return string.Empty;
        }

        public override string GetRsmNumber()
        {
            return "RSM011";
        }

        public override void SetPayloads(List<object> payloads)
        {
            PayloadNonContinuousEnergyTimeSeries = payloads.Cast<NonContinuousEnergyTimeSeries>().ToArray();
        }
    }
}
