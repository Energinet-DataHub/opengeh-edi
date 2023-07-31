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
using Domain.Transactions.AggregatedMeasureData.Events;
using NodaTime;

namespace Domain.Transactions.AggregatedMeasureData
{
    public class AggregatedMeasureDataProcess : Entity
    {
        private readonly ActorNumber _requestedByActorId;
        private State _state;

        public AggregatedMeasureDataProcess(
            ProcessId processId,
            BusinessTransactionId businessTransactionId,
            ActorNumber requestedByActorId,
            string? settlementVersion,
            string? meteringPointType,
            string? settlementMethod,
            Instant startOfPeriod,
            Instant? endOfPeriod,
            string? meteringGridAreaDomainId,
            string? energySupplierId,
            string? balanceResponsibleId)
        {
            ProcessId = processId;
            BusinessTransactionId = businessTransactionId;
            SettlementVersion = settlementVersion;
            MeteringPointType = meteringPointType;
            SettlementMethod = settlementMethod;
            StartOfPeriod = startOfPeriod;
            EndOfPeriod = endOfPeriod;
            MeteringGridAreaDomainId = meteringGridAreaDomainId;
            EnergySupplierId = energySupplierId;
            BalanceResponsibleId = balanceResponsibleId;
            _state = State.Initialized;
            _requestedByActorId = requestedByActorId;
            AddDomainEvent(new AggregatedMeasureProcessWasStarted(ProcessId));
        }

        public enum State
        {
            Initialized,
            Sent,
            Accepted, // TODO: LRN this would indicate that the process is completed, is only property to  describe state enough?
            Rejected,
        }

        public ProcessId ProcessId { get; }

        public BusinessTransactionId BusinessTransactionId { get; }

        /// <summary>
        /// Represent the version for a specific calculation.
        /// </summary>
        public string? SettlementVersion { get; }

        /// <summary>
        /// Represent consumption types or production.
        /// </summary>
        public string? MeteringPointType { get; }

        /// <summary>
        /// Represent the type of Settlement. E.g. Flex or NonProfile or null
        /// </summary>
        public string? SettlementMethod { get; }

        public Instant StartOfPeriod { get; }

        public Instant? EndOfPeriod { get; }

        public string? MeteringGridAreaDomainId { get; }

        public string? EnergySupplierId { get; }

        public string? BalanceResponsibleId { get; }

        public void WholesaleIsNotifiedOfRequest()
        {
            if (_state == State.Sent)
            {
                throw new AggregatedMeasureDataException("Wholesale has already been notified");
            }

            _state = State.Sent;
        }
    }
}
