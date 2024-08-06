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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices.ProcessEvents;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;

public sealed class WholesaleServicesProcess : Entity
{
    /// <summary>
    /// The process' grid areas are created when the process is created, and retrieved by Entity Framework from
    /// the WholesaleServicesProcessGridAreas table.
    /// </summary>
    private readonly IReadOnlyCollection<WholesaleServicesProcessGridArea> _gridAreas;

    private State _state = State.Initialized;

    /// <summary>
    /// Create a new process for wholesale services request, supplying both who the request is for and who requested it
    /// (this is used in case of delegation)
    /// </summary>
    public WholesaleServicesProcess(
        ProcessId processId,
        RequestedByActor requestedByActor,
        OriginalActor originalActor,
        TransactionId businessTransactionId,
        MessageId initiatedByMessageId,
        BusinessReason businessReason,
        string startOfPeriod,
        string? endOfPeriod,
        string? requestedGridArea,
        string? energySupplierId,
        SettlementVersion? settlementVersion,
        string? resolution,
        string? chargeOwner,
        IReadOnlyCollection<ChargeType> chargeTypes,
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
        InitiatedByMessageId = initiatedByMessageId;
        BusinessReason = businessReason;
        StartOfPeriod = startOfPeriod;
        EndOfPeriod = endOfPeriod;
        RequestedGridArea = requestedGridArea;
        EnergySupplierId = energySupplierId;
        SettlementVersion = settlementVersion;
        Resolution = resolution;
        ChargeOwner = chargeOwner;
        ChargeTypes = chargeTypes;
        _gridAreas = gridAreas.Select(ga => new WholesaleServicesProcessGridArea(Guid.NewGuid(), ProcessId, ga)).ToArray();
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

    public TransactionId BusinessTransactionId { get; }

    public MessageId InitiatedByMessageId { get; }

    public BusinessReason BusinessReason { get; }

    public string StartOfPeriod { get; }

    public string? EndOfPeriod { get; }

    /// <summary>
    /// The requested grid area is the grid area that was written in the request document, if this is null
    ///     then the request was for all appropriate grid areas.
    /// This value isn't used but is saved in the database for tracking. Always use the GridAreas list instead, since
    /// in case of delegation the request can be for multiple grid areas.
    /// </summary>
    public string? RequestedGridArea { get; }

    public string? EnergySupplierId { get; }

    public SettlementVersion? SettlementVersion { get; }

    public string? Resolution { get; }

    public string? ChargeOwner { get; }

    public IReadOnlyCollection<ChargeType> ChargeTypes { get; }

    /// <summary>
    /// Which grid area's the request is for. If this list is empty, then the request is for all appropriate grid areas.
    /// The process' grid areas are stored in the WholesaleServicesProcessGridAreas table.
    /// </summary>
    public IReadOnlyCollection<string> GridAreas => _gridAreas.Select(g => g.GridArea).ToArray();

    public void SendToWholesale()
    {
        if (_state != State.Initialized)
            return;

        AddDomainEvent(new NotifyWholesaleThatWholesaleServicesIsRequested(this));

        _state = State.Sent;
    }

    public void IsAccepted(IReadOnlyCollection<AcceptedWholesaleServicesMessageDto> acceptedWholesaleServicesMessages)
    {
        ArgumentNullException.ThrowIfNull(acceptedWholesaleServicesMessages);

        if (_state != State.Sent)
            return;

        foreach (var acceptedWholesaleServicesMessage in acceptedWholesaleServicesMessages)
        {
            AddDomainEvent(new EnqueuedAcceptedWholesaleServicesEvent(acceptedWholesaleServicesMessage));
        }

        _state = State.Accepted;
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
}
