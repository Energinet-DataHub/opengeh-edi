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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices.ProcessEvents;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;

public class WholesaleServicesProcess : Entity
{
    private State _state = State.Initialized;

    public WholesaleServicesProcess(
        ProcessId processId,
        ActorNumber requestedByActorId,
        string requestedByActorRoleCode,
        BusinessTransactionId businessTransactionId,
        MessageId initiatedByMessageId,
        BusinessReason businessReason,
        string startOfPeriod,
        string? endOfPeriod,
        string? gridAreaCode,
        string? energySupplierId,
        SettlementVersion? settlementVersion,
        string? resolution,
        string? chargeOwner,
        IReadOnlyCollection<ChargeType> chargeTypes)
    {
        ProcessId = processId;
        RequestedByActorId = requestedByActorId;
        RequestedByActorRoleCode = requestedByActorRoleCode;
        BusinessTransactionId = businessTransactionId;
        InitiatedByMessageId = initiatedByMessageId;
        BusinessReason = businessReason;
        StartOfPeriod = startOfPeriod;
        EndOfPeriod = endOfPeriod;
        GridAreaCode = gridAreaCode;
        EnergySupplierId = energySupplierId;
        SettlementVersion = settlementVersion;
        Resolution = resolution;
        ChargeOwner = chargeOwner;
        ChargeTypes = chargeTypes;
        AddDomainEvent(new WholesaleServicesProcessIsInitialized(processId));
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
    private WholesaleServicesProcess(State state)
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

    public ActorNumber RequestedByActorId { get; }

    public string RequestedByActorRoleCode { get; }

    public BusinessTransactionId BusinessTransactionId { get; }

    public MessageId InitiatedByMessageId { get; }

    public BusinessReason BusinessReason { get; }

    public string StartOfPeriod { get; }

    public string? EndOfPeriod { get; }

    public string? GridAreaCode { get; }

    public string? EnergySupplierId { get; }

    public SettlementVersion? SettlementVersion { get; }

    public string? Resolution { get; }

    public string? ChargeOwner { get; }

    public IReadOnlyCollection<ChargeType> ChargeTypes { get; }

    public void SendToWholesale()
    {
        if (_state == State.Initialized)
        {
            AddDomainEvent(new NotifyWholesaleThatWholesaleServicesIsRequested(this));
            _state = State.Sent;
        }
    }

    public void IsAccepted(IReadOnlyCollection<AcceptedWholesaleServicesMessageDto> acceptedWholesaleServicesMessages)
    {
        ArgumentNullException.ThrowIfNull(acceptedWholesaleServicesMessages);

        if (_state == State.Sent)
        {
            foreach (var acceptedWholesaleServicesMessage in acceptedWholesaleServicesMessages)
            {
                AddDomainEvent(new EnqueuedAcceptedWholesaleServicesEvent(acceptedWholesaleServicesMessage));
            }

            _state = State.Accepted;
        }
    }

    public void IsRejected(RejectedWholesaleServicesMessageDto rejectedWholesaleServicesRequest)
    {
        ArgumentNullException.ThrowIfNull(rejectedWholesaleServicesRequest);

        if (_state != State.Sent)
        {
            return;
        }

        AddDomainEvent(new EnqueueRejectedWholesaleServicesMessageEvent(rejectedWholesaleServicesRequest));

        _state = State.Rejected;
    }
}
