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
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Domain.GridAreaOwners;

/// <summary>
/// Responsible for accessing a grid area.
/// </summary>
public interface IGridAreaRepository
{
    /// <summary>
    /// Responsible for creating a grid area if it does not exist.
    /// </summary>
    Task UpdateOwnershipAsync(
        string gridAreaCode,
        Instant validFrom,
        ActorNumber actorNumber,
        int sequenceNumber,
        CancellationToken cancellationToken);

    /// <summary>
    /// Responsible for getting the grid operator for a given grid area.
    /// </summary>
    Task<ActorNumber?> GetGridOwnerForAsync(string gridAreaCode, CancellationToken cancellationToken);
}
