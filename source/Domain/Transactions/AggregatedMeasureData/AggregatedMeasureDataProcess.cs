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

using Domain.Actors;
using Domain.SeedWork;
using NodaTime;

namespace Domain.Transactions.AggregatedMeasureData
{
    public class AggregatedMeasureDataProcess : Entity
    {
#pragma warning disable CS0414
        private readonly State _state;
#pragma warning restore CS0414

        private readonly ActorNumber _requestedByActorId;

        public AggregatedMeasureDataProcess(
            ProcessId processId,
            string settlementSeriesVersion,
            string marketEvaluationPointType,
            string marketEvaluationSettlementMethod,
            Instant startDateAndOrTimeDateTime,
            Instant endDateAndOrTimeDateTime,
            string meteringGridAreaDomainId,
            string biddingZoneDomainId,
            string energySupplierMarketParticipantId,
            string balanceResponsiblePartyMarketParticipantId,
            ActorNumber requestedByActorId)
        {
            ProcessId = processId;
            SettlementSeriesVersion = settlementSeriesVersion;
            MarketEvaluationPointType = marketEvaluationPointType;
            MarketEvaluationSettlementMethod = marketEvaluationSettlementMethod;
            StartDateAndOrTimeDateTime = startDateAndOrTimeDateTime;
            EndDateAndOrTimeDateTime = endDateAndOrTimeDateTime;
            MeteringGridAreaDomainId = meteringGridAreaDomainId;
            BiddingZoneDomainId = biddingZoneDomainId;
            EnergySupplierMarketParticipantId = energySupplierMarketParticipantId;
            BalanceResponsiblePartyMarketParticipantId = balanceResponsiblePartyMarketParticipantId;
            _requestedByActorId = requestedByActorId;
            _state = State.Initialized;
        }

        public enum State
        {
            Initialized,
            BeingProcessed,
            Rejected,
            Accepted,
            Completed,
        }

        public ProcessId ProcessId { get; }

        public string SettlementSeriesVersion { get; }

        public string MarketEvaluationPointType { get; }

        public string MarketEvaluationSettlementMethod { get; }

        public Instant StartDateAndOrTimeDateTime { get; }

        public Instant EndDateAndOrTimeDateTime { get; }

        public string MeteringGridAreaDomainId { get; }

        public string BiddingZoneDomainId { get; }

        public string EnergySupplierMarketParticipantId { get; }

        public string BalanceResponsiblePartyMarketParticipantId { get; }
    }
}
