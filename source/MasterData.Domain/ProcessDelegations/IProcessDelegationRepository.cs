﻿// Copyright 2020 Energinet DataHub A/S
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

namespace Energinet.DataHub.EDI.MasterData.Domain.ProcessDelegations;

/// <summary>
///     Process delegation repository
/// </summary>
public interface IProcessDelegationRepository
{
    /// <summary>
    ///     Create a process delegation
    /// </summary>
    void Create(ProcessDelegation processDelegation, CancellationToken cancellationToken);

    /// <summary>
    ///     Get the latest delegation from today for the given actor number, actor role, grid area code and process type.
    /// </summary>
    Task<ProcessDelegation?> GetAsync(
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        string? gridAreaCode,
        ProcessType processType,
        CancellationToken cancellationToken);
}
