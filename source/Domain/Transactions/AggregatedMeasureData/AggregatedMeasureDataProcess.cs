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

using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Common;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;

namespace Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData
{
    public class AggregatedMeasureDataProcess : Entity
    {
        private readonly List<OutgoingMessage> _messages = new();
        private readonly List<PendingAggregation> _pendingMessages = new();
        private State _state = State.Initialized;

        public AggregatedMeasureDataProcess(
            ProcessId processId,
            BusinessTransactionId businessTransactionId,
            ActorNumber requestedByActorId,
            string requestedByActorRoleCode,
            string businessReason,
            string? meteringPointType,
            string? settlementMethod,
            string startOfPeriod,
            string? endOfPeriod,
            string? meteringGridAreaDomainId,
            string? energySupplierId,
            string? balanceResponsibleId,
            string? settlementVersion)
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

        public string BusinessReason { get; }

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

        public string? SettlementVersion { get; }

        public ActorNumber RequestedByActorId { get; set; }

        public string RequestedByActorRoleCode { get; }

        public void WasSentToWholesale()
        {
            if (_state == State.Initialized)
            {
                _state = State.Sent;
            }
        }

        public void AddResponseMessage(Aggregation aggregation)
        {
            if (aggregation == null) throw new ArgumentNullException(nameof(aggregation));

            if (_state == State.Sent)
            {
                if (_pendingMessages.All(message =>
                        message.GridAreaDetails.GridAreaCode != aggregation.GridAreaDetails.GridAreaCode))
                {
                    _pendingMessages.Add(MapAggregationToPending(aggregation));
                }
            }
        }

        public void IsRejected(RejectedAggregatedMeasureDataRequest rejectAggregatedMeasureDataRequest)
        {
            if (rejectAggregatedMeasureDataRequest == null) throw new ArgumentNullException(nameof(rejectAggregatedMeasureDataRequest));

            if (_state == State.Sent)
            {
                _messages.Add(CreateRejectedAggregationResultMessage(rejectAggregatedMeasureDataRequest));

                _state = State.Rejected;
            }
        }

        public void IsAccepted(IReadOnlyList<string> gridAreas)
        {
            if (gridAreas == null) throw new ArgumentNullException(nameof(gridAreas));
            if (_state != State.Sent)
            {
                return;
            }

            if (gridAreas.Count != _pendingMessages.Count)
            {
                if (HaveToClearPendingMessages(gridAreas))
                {
                    _pendingMessages.Clear();
                }

                _state = State.Initialized;
                AddDomainEvent(new AggregatedMeasureProcessIsInitialized(ProcessId));
                return;
            }

            foreach (var message in _pendingMessages)
            {
                _messages.Add(AggregationResultMessageFactory.CreateMessage(MapPendingToAggregation(message), ProcessId));
            }

            _pendingMessages.Clear();
            _state = State.Accepted;
        }

        private static Aggregation MapPendingToAggregation(PendingAggregation aggregation)
        {
            return new Aggregation(
                aggregation.Points.Select(point =>
                    new Energinet.DataHub.EDI.Domain.Transactions.Aggregations.Point(
                        point.Position,
                        point.Quantity,
                        point.Quality,
                        point.SampleTime)).ToList(),
                aggregation.MeteringPointType,
                aggregation.MeasureUnitType,
                aggregation.Resolution,
                new Energinet.DataHub.EDI.Domain.Transactions.Aggregations.Period(
                    aggregation.Period.Start,
                    aggregation.Period.End),
                aggregation.SettlementType,
                aggregation.BusinessReason,
                new Energinet.DataHub.EDI.Domain.Transactions.Aggregations.ActorGrouping(
                    aggregation.ActorGrouping.EnergySupplierNumber, aggregation.ActorGrouping.BalanceResponsibleNumber),
                new Energinet.DataHub.EDI.Domain.Transactions.Aggregations.GridAreaDetails(
                    aggregation.GridAreaDetails.GridAreaCode, aggregation.GridAreaDetails.OperatorNumber),
                aggregation.OriginalTransactionIdReference,
                aggregation.Receiver,
                aggregation.ReceiverRole,
                aggregation.SettlementVersion);
        }

        private bool HaveToClearPendingMessages(IReadOnlyList<string> gridAreas)
        {
            if (_pendingMessages.Count <= gridAreas.Count && _pendingMessages.All(message => gridAreas.Contains(message.GridAreaDetails.GridAreaCode)))
            {
                return false;
            }

            return true;
        }

        private RejectedAggregationResultMessage CreateRejectedAggregationResultMessage(
            RejectedAggregatedMeasureDataRequest rejectedAggregatedMeasureDataRequest)
        {
            var rejectedTimeSerie = new RejectedTimeSerie(
                ProcessId.Id,
                rejectedAggregatedMeasureDataRequest.RejectReasons.Select(reason =>
                        new Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData.RejectReason(
                            reason.ErrorCode,
                            reason.ErrorMessage))
                    .ToList(),
                BusinessTransactionId.Id);

            return new RejectedAggregationResultMessage(
                RequestedByActorId,
                ProcessId,
                rejectedAggregatedMeasureDataRequest.BusinessReason.Name,
                MarketRole.FromCode(RequestedByActorRoleCode),
                rejectedTimeSerie);
        }

        private PendingAggregation MapAggregationToPending(Aggregation aggregation)
        {
            return new(
                aggregation.Points.Select(point =>
                    new Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.Point(
                        point.Position,
                        point.Quantity,
                        point.Quality,
                        point.SampleTime)).ToList(),
                aggregation.MeteringPointType,
                aggregation.MeasureUnitType,
                aggregation.Resolution,
                aggregation.SettlementType,
                aggregation.BusinessReason,
                ProcessId,
                aggregation.Period.Start,
                aggregation.Period.End,
                aggregation.GridAreaDetails.GridAreaCode,
                aggregation.GridAreaDetails.OperatorNumber,
                aggregation.SettlementVersion,
                aggregation.ReceiverRole,
                aggregation.Receiver,
                aggregation.OriginalTransactionIdReference,
                aggregation.ActorGrouping.EnergySupplierNumber,
                aggregation.ActorGrouping.BalanceResponsibleNumber);
        }
    }
}
