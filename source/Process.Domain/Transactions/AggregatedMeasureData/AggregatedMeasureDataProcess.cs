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
using System.Linq;
using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData
{
    public class AggregatedMeasureDataProcess : Entity
    {
        private State _state = State.Initialized;

        public AggregatedMeasureDataProcess(
            ProcessId processId,
            BusinessTransactionId businessTransactionId,
            ActorNumber requestedByActorId,
            string requestedByActorRoleCode,
            BusinessReason businessReason,
            string? meteringPointType,
            string? settlementMethod,
            string startOfPeriod,
            string? endOfPeriod,
            string? meteringGridAreaDomainId,
            string? energySupplierId,
            string? balanceResponsibleId,
            SettlementVersion? settlementVersion)
        {
            ProcessId = processId;
            BusinessTransactionId = businessTransactionId;
            BusinessReason = businessReason;
            MeteringPointType = meteringPointType;
            SettlementMethod = settlementMethod;
            StartOfPeriod = startOfPeriod;
            EndOfPeriod = endOfPeriod;
            MeteringGridAreaDomainId = meteringGridAreaDomainId;
            EnergySupplierId = energySupplierId;
            BalanceResponsibleId = balanceResponsibleId;
            SettlementVersion = settlementVersion;
            RequestedByActorId = requestedByActorId;
            RequestedByActorRoleCode = requestedByActorRoleCode;
            AddDomainEvent(new AggregatedMeasureProcessIsInitialized(processId));
        }

        /// <summary>
        /// DO NOT DELETE THIS OR CREATE A CONSTRUCTOR WITH LESS PARAMETERS.
        /// Entity Framework needs this, since it uses the constructor with the least parameters.
        /// Thereafter assign the rest of the parameters via reflection.
        /// To avoid throwing domainEvents when EF loads entity from database
        /// </summary>
        /// <param name="state"></param>
        /// <remarks> Dont use this! </remarks>
#pragma warning disable CS8618
        private AggregatedMeasureDataProcess(State state)
#pragma warning restore CS8618
        {
            _state = state;
        }

        public enum State
        {
            Initialized,
            Sent,
            Accepted,
            Rejected,
        }

        public ProcessId ProcessId { get; }

        public BusinessTransactionId BusinessTransactionId { get; }

        public BusinessReason BusinessReason { get; }

        /// <summary>
        /// Represent consumption types or production.
        /// </summary>
        public string? MeteringPointType { get; }

        /// <summary>
        /// Represent the type of Settlement. E.g. Flex or NonProfile or null
        /// </summary>
        public string? SettlementMethod { get; }

        public string StartOfPeriod { get; }

        public string? EndOfPeriod { get; }

        public string? MeteringGridAreaDomainId { get; }

        public string? EnergySupplierId { get; }

        public string? BalanceResponsibleId { get; }

        public SettlementVersion? SettlementVersion { get; }

        public ActorNumber RequestedByActorId { get; set; }

        public string RequestedByActorRoleCode { get; }

        public void WasSentToWholesale()
        {
            if (_state == State.Initialized)
            {
                _state = State.Sent;
            }
        }

        public void IsAccepted(Aggregation aggregation)
        {
            if (aggregation == null) throw new ArgumentNullException(nameof(aggregation));

            if (_state == State.Sent)
            {
                AddDomainEvent(new EnqueueMessageEvent(AggregationResultMessageFactory.CreateMessage(aggregation, ProcessId)));
                _state = State.Accepted;
            }
        }

        public void IsRejected(RejectedAggregatedMeasureDataRequest rejectAggregatedMeasureDataRequest)
        {
            if (rejectAggregatedMeasureDataRequest == null) throw new ArgumentNullException(nameof(rejectAggregatedMeasureDataRequest));

            if (_state == State.Sent)
            {
                AddDomainEvent(new EnqueueMessageEvent(CreateRejectedAggregationResultMessage(rejectAggregatedMeasureDataRequest)));
                _state = State.Rejected;
            }
        }

        private RejectedAggregationResultMessage CreateRejectedAggregationResultMessage(
            RejectedAggregatedMeasureDataRequest rejectedAggregatedMeasureDataRequest)
        {
            var rejectedTimeSerie = new RejectedTimeSerie(
                ProcessId.Id,
                rejectedAggregatedMeasureDataRequest.RejectReasons.Select(reason =>
                        new Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.OutgoingMessages.RejectReason(
                            reason.ErrorCode,
                            reason.ErrorMessage))
                    .ToList(),
                BusinessTransactionId.Id);

            return new RejectedAggregationResultMessage(
                RequestedByActorId,
                ProcessId.Id,
                rejectedAggregatedMeasureDataRequest.BusinessReason.Name,
                MarketRole.FromCode(RequestedByActorRoleCode),
                rejectedTimeSerie);
        }
    }
}
