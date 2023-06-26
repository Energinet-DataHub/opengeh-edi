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

using System.Data;
using Domain.SeedWork;

namespace Domain.Transactions.AggregatedMeasureData
{
    public class AggregatedMeasureDataProcess : Entity
    {
        private readonly string _id;
        private readonly string _settlementSeriesVersion;
        private readonly string _marketEvaluationPointType;
        private readonly string _marketEvaluationSettlementMethod;
        private readonly string _startDateAndOrTimeDateTime;
        private readonly string _endDateAndOrTimeDateTime;
        private readonly string _meteringGridAreaDomainId;
        private readonly string _biddingZoneDomainId;
        private readonly string _energySupplierMarketParticipantId;
        private readonly string _balanceResponsiblePartyMarketParticipantId;
        private readonly string _requestedByActorId;
#pragma warning disable CS0414
        private readonly State _state;
#pragma warning restore CS0414

        public AggregatedMeasureDataProcess(string id, string settlementSeriesVersion, string marketEvaluationPointType, string marketEvaluationSettlementMethod, string startDateAndOrTimeDateTime, string endDateAndOrTimeDateTime, string meteringGridAreaDomainId, string biddingZoneDomainId, string energySupplierMarketParticipantId, string balanceResponsiblePartyMarketParticipantId, string requestedByActorId)
        {
            _id = id;
            _settlementSeriesVersion = settlementSeriesVersion;
            _marketEvaluationPointType = marketEvaluationPointType;
            _marketEvaluationSettlementMethod = marketEvaluationSettlementMethod;
            _startDateAndOrTimeDateTime = startDateAndOrTimeDateTime;
            _endDateAndOrTimeDateTime = endDateAndOrTimeDateTime;
            _meteringGridAreaDomainId = meteringGridAreaDomainId;
            _biddingZoneDomainId = biddingZoneDomainId;
            _energySupplierMarketParticipantId = energySupplierMarketParticipantId;
            _balanceResponsiblePartyMarketParticipantId = balanceResponsiblePartyMarketParticipantId;
            _requestedByActorId = requestedByActorId;
            _state = State.Started;
        }

        public enum State
        {
            Started,
            Accepted,
            Rejected,
            Completed,
        }
    }
}
