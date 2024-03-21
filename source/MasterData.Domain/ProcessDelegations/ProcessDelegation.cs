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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Domain.ProcessDelegations;

/// <summary>
/// A process delegation is used when one actor wishes to delegated a process to another actor.
/// An example: Actor A wants Actor B to receive all their energy results.
/// </summary>
public class ProcessDelegation
{
    private readonly Guid _id;

    public ProcessDelegation(
        int sequenceNumber,
        string delegatedProcess,
        string gridAreaCode,
        Instant startsAt,
        Instant stopsAt,
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        ActorNumber delegatedToActorNumber,
        ActorRole delegatedToActorRole)
    {
        _id = Guid.NewGuid();
        SequenceNumber = sequenceNumber;
        DelegatedProcess = delegatedProcess;
        GridAreaCode = gridAreaCode;
        StartsAt = startsAt;
        StopsAt = stopsAt;
        DelegatedByActorNumber = delegatedByActorNumber;
        DelegatedByActorRole = delegatedByActorRole;
        DelegatedToActorNumber = delegatedToActorNumber;
        DelegatedToActorRole = delegatedToActorRole;
    }

#pragma warning disable CS8618 // Needed by Entity Framework
    private ProcessDelegation()
    {
    }
#pragma warning restore CS8618 // Needed by Entity Framework

    /// <summary>
    /// Used to determine the latest delegation configuration.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The type of process that is delegated ex: PROCESS_REQUEST_ENERGY_RESULTS
    /// </summary>
    public string DelegatedProcess { get; }

    /// <summary>
    /// The code of the grid area for which the process is delegated.
    /// </summary>
    public string GridAreaCode { get; set; }

    /// <summary>
    /// The start timestamp of the configured delegation (inclusive).
    /// </summary>
    public Instant StartsAt { get; set; }

    /// <summary>
    /// The end timestamp of the configured delegation (inclusive).
    /// If the delegation does not stop, stops_at is set to December 31, 9999.
    /// If stops_at occurs before starts_at, then the delegation is cancelled.
    /// </summary>
    public Instant StopsAt { get; set; }

    /// <summary>
    /// The EIC or GLN identifier of the actor that delegated its process to another actor.
    /// </summary>
    public ActorNumber DelegatedByActorNumber { get; set; }

    public ActorRole DelegatedByActorRole { get; set; }

    public ActorNumber DelegatedToActorNumber { get; set; }

    public ActorRole DelegatedToActorRole { get; set; }
}
