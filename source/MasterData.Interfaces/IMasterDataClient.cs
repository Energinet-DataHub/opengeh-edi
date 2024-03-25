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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Interfaces;

/// <summary>
///     Client definition for the master data module.
/// </summary>
public interface IMasterDataClient
{
    /// <summary>
    ///     Create a new actor, assuming it does not already exist.
    /// </summary>
    Task CreateActorIfNotExistAsync(CreateActorDto createActorDto, CancellationToken cancellationToken);

    /// <summary>
    ///     Get the <see cref="ActorNumber" /> of the Actor with the provided external id.
    /// </summary>
    Task<ActorNumber?> GetActorNumberByExternalIdAsync(string externalId, CancellationToken cancellationToken);

    /// <summary>
    ///     Create a new grid area owner entry.
    /// </summary>
    Task UpdateGridAreaOwnershipAsync(
        GridAreaOwnershipAssignedDto gridAreaOwnershipAssignedDto,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Get the <see cref="ActorNumber"/> of the grid operator for a given grid area.
    /// </summary>
    Task<ActorNumber> GetGridOwnerForGridAreaCodeAsync(string gridAreaCode, CancellationToken cancellationToken);

    /// <summary>
    ///     Create or update the actor certificate for a given actor.
    /// </summary>
    Task CreateOrUpdateActorCertificateAsync(
        ActorCertificateCredentialsAssignedDto request,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the <see cref="ActorNumberAndRoleDto" /> associated with the given certificate thumbprint, if any.
    /// </summary>
    Task<ActorNumberAndRoleDto?> GetActorNumberAndRoleFromThumbprintAsync(CertificateThumbprintDto thumbprintDto);

    /// <summary>
    ///     Delete the actor certificate for a given actor.
    /// </summary>
    Task DeleteActorCertificateAsync(
        ActorCertificateCredentialsRemovedDto actorCertificateCredentialsRemovedDto,
        CancellationToken cancellationToken);

    /// <summary>
    ///    Create a new process delegation.
    /// </summary>
    Task CreateProcessDelegationAsync(ProcessDelegationDto processDelegationDto, CancellationToken cancellationToken);

    /// <summary>
    ///    Get process delegation.
    /// </summary>
    Task<ProcessDelegationDto?> GetProcessDelegationAsync(
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        string gridAreaCode,
        ProcessType processType,
        CancellationToken cancellationToken);
}
