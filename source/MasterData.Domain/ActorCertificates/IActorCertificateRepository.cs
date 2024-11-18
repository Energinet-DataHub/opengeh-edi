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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.MasterData.Domain.ActorCertificates;

/// <summary>
/// Repository for looking up actor certificates
/// </summary>
public interface IActorCertificateRepository
{
    /// <summary>
    /// Get actor certificate from thumbprint. Returns null if no actor certificate was found.
    /// </summary>
    Task<ActorCertificate?> GetFromThumbprintAsync(CertificateThumbprint thumbprint);

    /// <summary>
    /// Get actor certificate from the actor and role combination. Returns null if no actor certificate was found for the given combination.
    /// </summary>
    Task<ActorCertificate?> GetFromActorRoleAsync(ActorNumber actorNumber, ActorRole actorRole);

    /// <summary>
    /// Add the actor certificate to storage
    /// </summary>
    void Add(ActorCertificate newActorCertificate);

    /// <summary>
    /// Delete the actor certificate for specified actor and certificate thumbprint
    /// </summary>
    Task DeleteAsync(ActorNumber actorNumber, CertificateThumbprint certificateThumbprint, CancellationToken cancellationToken);
}
