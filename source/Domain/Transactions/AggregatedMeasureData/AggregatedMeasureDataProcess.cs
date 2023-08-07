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
        private State _state = State.Initialized;

        public AggregatedMeasureDataProcess(
            ProcessId processId,
            BusinessTransactionId businessTransactionId,
            ActorNumber requestedByActorId,
            string requestedByActorRoleCode,
            string businessReason,
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
            BusinessReason = businessReason;
            SettlementVersion = settlementVersion;
            MeteringPointType = meteringPointType;
            SettlementMethod = settlementMethod;
            StartOfPeriod = startOfPeriod;
            EndOfPeriod = endOfPeriod;
            MeteringGridAreaDomainId = meteringGridAreaDomainId;
            EnergySupplierId = energySupplierId;
            BalanceResponsibleId = balanceResponsibleId;
            RequestedByActorId = requestedByActorId;
            RequestedByActorRoleCode = requestedByActorRoleCode;

            //Ensures that ORM doesn't create domain event when loading this entity.
            if (_state == State.Initialized)
            {
                AddDomainEvent(new AggregatedMeasureProcessIsStarted(processId));
            }
        }

        public enum State
        {
            Initialized,
            Sending,
            Sent,
            Accepted, // TODO: LRN this would indicate that the process is completed, is only property to  describe state enough?
            Rejected,
        }

        public ProcessId ProcessId { get; }

        public BusinessTransactionId BusinessTransactionId { get; }

        public string BusinessReason { get; }

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

        public ActorNumber RequestedByActorId { get; set; }

        public string RequestedByActorRoleCode { get; }

        public string? ResponseData { get; set; }

        public void SendToWholesale()
        {
            if (_state == State.Initialized)
            {
                _state = State.Sending;
                AddDomainEvent(new AggregatedMeasureProcessIsSending(ProcessId));
            }
        }

        public void WasSentToWholesale()
        {
            if (_state == State.Sending)
            {
                _state = State.Sent;
            }
        }

        public void WasAccepted(string responseData)
        {
            if (_state == State.Sent)
            {
                _state = State.Accepted;
                ResponseData = responseData;
                AddDomainEvent(new AggregatedMeasureProcessWasAccepted(ProcessId));
            }
        }
    }
}
