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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData
{
    public sealed class AggregatedMeasureDataProcess : Entity
    {
        /// <summary>
        /// The process' grid areas are created when the process is created, and retrieved by Entity Framework from
        /// the WholesaleServicesProcessGridAreas table.
        /// </summary>
        private readonly IReadOnlyCollection<AggregatedMeasureDataProcessGridArea> _gridAreas;

        private State _state = State.Initialized;

        public AggregatedMeasureDataProcess(
            ProcessId processId,
            RequestedByActor requestedByActor,
            OriginalActor originalActor,
            BusinessTransactionId businessTransactionId,
            BusinessReason businessReason,
            MessageId initiatedByMessageId,
            string? meteringPointType,
            string? settlementMethod,
            string startOfPeriod,
            string? endOfPeriod,
            string? requestedGridArea,
            string? energySupplierId,
            string? balanceResponsibleId,
            SettlementVersion? settlementVersion,
            IReadOnlyCollection<string> gridAreas)
        {
            ArgumentNullException.ThrowIfNull(gridAreas);
            ArgumentNullException.ThrowIfNull(processId);

            if (!GridAreasAreInSyncWithRequestedGridArea(requestedGridArea, gridAreas))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gridAreas),
                    gridAreas,
                    $"Grid areas must contain exactly the requested grid area when the requested grid area is not null (id: {processId.Id})");
            }

            ProcessId = processId;
            RequestedByActor = requestedByActor;
            OriginalActor = originalActor;
            BusinessTransactionId = businessTransactionId;
            BusinessReason = businessReason;
            InitiatedByMessageId = initiatedByMessageId;
            MeteringPointType = meteringPointType;
            SettlementMethod = settlementMethod;
            StartOfPeriod = startOfPeriod;
            EndOfPeriod = endOfPeriod;
            RequestedGridArea = requestedGridArea;
            EnergySupplierId = energySupplierId;
            BalanceResponsibleId = balanceResponsibleId;
            SettlementVersion = settlementVersion;
            _gridAreas = gridAreas.Select(ga => new AggregatedMeasureDataProcessGridArea(Guid.NewGuid(), ProcessId, ga)).ToArray();
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

        /// <summary>
        /// The actor that requested the wholesale services (the sender of the request). This is typically the actor
        /// that owns the request/process, except in case of delegation.
        /// </summary>
        public RequestedByActor RequestedByActor { get; }

        /// <summary>
        /// The original actor is the actor that the wholesale services is requested for (who owns the request/process)
        /// This can differ from RequestedByActorNumber in case of delegation
        /// </summary>
        public OriginalActor OriginalActor { get; }

        public BusinessTransactionId BusinessTransactionId { get; }

        public BusinessReason BusinessReason { get; }

        /// <summary>
        /// Message id of the request staring the process(s)
        /// </summary>
        public MessageId InitiatedByMessageId { get; }

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

        public string? RequestedGridArea { get; }

        /// <summary>
        /// Which grid area's the request is for. If this list is empty, then the request is for all appropriate grid areas.
        /// The process' grid areas are stored in the AggregatedMeasureDataProcessGridAreas table.
        /// </summary>
        public IReadOnlyCollection<string> GridAreas => _gridAreas.Select(g => g.GridArea).ToArray();

        public string? EnergySupplierId { get; }

        public string? BalanceResponsibleId { get; }

        public SettlementVersion? SettlementVersion { get; }

        public void SendToWholesale()
        {
            if (_state != State.Initialized)
                return;

            AddDomainEvent(new NotifyWholesaleThatAggregatedMeasureDataIsRequested(this));

            _state = State.Sent;
        }

        public void IsAccepted(IReadOnlyCollection<AcceptedEnergyResultMessageDto> acceptedEnergyResultMessages)
        {
            ArgumentNullException.ThrowIfNull(acceptedEnergyResultMessages);

            if (_state != State.Sent)
                return;

            foreach (var acceptedEnergyResultMessage in acceptedEnergyResultMessages)
            {
                AddDomainEvent(new EnqueueAcceptedEnergyResultMessageEvent(acceptedEnergyResultMessage));
            }

            _state = State.Accepted;
        }

        public void IsRejected(RejectedAggregatedMeasureDataRequest rejectAggregatedMeasureDataRequest)
        {
            ArgumentNullException.ThrowIfNull(rejectAggregatedMeasureDataRequest);

            if (_state != State.Sent)
                return;

            AddDomainEvent(new EnqueueRejectedEnergyResultMessageEvent(CreateRejectedAggregationResultMessage(rejectAggregatedMeasureDataRequest)));

            _state = State.Rejected;
        }

        /// <summary>
        /// If requested grid are has a value, then grid areas must contain exactly the requested grid area.
        /// </summary>
        private bool GridAreasAreInSyncWithRequestedGridArea(string? requestedGridArea, IReadOnlyCollection<string> gridAreas)
        {
            // If requested grid area is null, then grid areas can have any value
            if (string.IsNullOrEmpty(requestedGridArea))
                return true;

            // If requested grid area is not null, then grid areas must contain exactly the requested grid area
            return gridAreas.Count == 1 && gridAreas.Single() == requestedGridArea;
        }

        private RejectedEnergyResultMessageDto CreateRejectedAggregationResultMessage(
            RejectedAggregatedMeasureDataRequest rejectedAggregatedMeasureDataRequest)
        {
            var rejectedTimeSerie = new RejectedEnergyResultMessageSerie(
                ProcessId.Id,
                rejectedAggregatedMeasureDataRequest.RejectReasons.Select(reason =>
                        new RejectedEnergyResultMessageRejectReason(
                            reason.ErrorCode,
                            reason.ErrorMessage))
                    .ToList(),
                BusinessTransactionId.Id);

            return new RejectedEnergyResultMessageDto(
                receiverNumber: RequestedByActor.ActorNumber,
                processId: ProcessId.Id,
                eventId: rejectedAggregatedMeasureDataRequest.EventId,
                businessReason: rejectedAggregatedMeasureDataRequest.BusinessReason.Name,
                receiverRole: RequestedByActor.ActorRole,
                relatedToMessageId: InitiatedByMessageId,
                series: rejectedTimeSerie,
                documentReceiverNumber: OriginalActor.ActorNumber,
                documentReceiverRole: OriginalActor.ActorRole);
        }
    }
}
