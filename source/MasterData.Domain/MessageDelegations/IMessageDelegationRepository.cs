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

namespace Energinet.DataHub.EDI.MasterData.Domain.MessageDelegations;

/// <summary>
///     Message delegation repository
/// </summary>
public interface IMessageDelegationRepository
{
    /// <summary>
    ///     Create a message delegation
    /// </summary>
    Task CreateAsync(MessageDelegation messageDelegation, CancellationToken cancellationToken);

    /// <summary>
    ///     Get a message delegation
    /// </summary>
    Task<MessageDelegation?> GetAsync(ActorNumber delegatedByActorNumber, ActorRole delegatedByActorRole, string gridAreaCode, DocumentType documentType, Instant now);
}
