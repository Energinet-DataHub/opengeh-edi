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

namespace Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.RSM.RSM023.V3.Response
{
    public partial class DK_ResponseMasterDataMeteringPointType : RsmDocument
    {
        public override int PayloadCount
        {
            get { return PayloadMasterDataMPEvent.Length; }
        }

        public static DK_ResponseMasterDataMeteringPointType? BuildPayload(RsmDocument emptyResponse, Collection<Tuple<string, string>> args)
        {
            ArgumentNullException.ThrowIfNull(args, nameof(args));

            var header = new EnergyDocument
            {
                Identification = args[5].Item2,
            };
            var process = new EnergyContext
            {
                EnergyBusinessProcess = new EnergyContextEnergyBusinessProcess(),
            };
            process.EnergyBusinessProcess.Value = args[2].Item2;
            process.EnergyBusinessProcess.listAgencyIdentifier = BusinessReasonCodeType_000115ListAgencyIdentifier.Item260;
            var returnResponse = emptyResponse as DK_ResponseMasterDataMeteringPointType;

            var objChild = new MasterDataMPEvent
            {
                OriginalBusinessDocument = args[2].Item2,
                MeteringPointDomainLocation = new DomainLocation(),
            };
            objChild.MeteringPointDomainLocation.Identification = new DomainIdentifierType_000122
            {
                Value = args[4].Item2,
            };

            if (returnResponse != null)
            {
                returnResponse.PayloadMasterDataMPEvent = new MasterDataMPEvent[1];
                returnResponse.PayloadMasterDataMPEvent[0] = objChild;
                returnResponse.HeaderEnergyDocument = header;
                returnResponse.ProcessEnergyContext = process;
            }

            return returnResponse;
        }

        public override void AddPayload(object payload)
        {
            var payloads = PayloadMasterDataMPEvent.ToList();
            payloads.Add((MasterDataMPEvent)payload);
            PayloadMasterDataMPEvent = payloads.ToArray();
        }

#nullable enable
#nullable disable

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
            return "ResponseMasterDataMeteringPoint";
        }

        public override string GetMessageId()
        {
            return HeaderEnergyDocument.Identification;
        }

        public override DateTime? GetOccurrence(int payloadIndex)
        {
            return PayloadMasterDataMPEvent[payloadIndex].Occurrence;
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
            return PayloadMasterDataMPEvent[payloadIndex].OriginalBusinessDocument;
        }

        public override string GetResponseCode(int payloadIndex)
        {
            return string.Empty;
        }

        public override string GetRsmNumber()
        {
            return "RSM023";
        }

        public override void SetPayloads(Collection<object> payloads)
        {
            PayloadMasterDataMPEvent = payloads.Cast<MasterDataMPEvent>().ToArray();
        }
    }
}
