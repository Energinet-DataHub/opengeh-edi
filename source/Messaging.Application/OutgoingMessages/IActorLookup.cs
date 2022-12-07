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
using System.Threading.Tasks;
using Messaging.Domain.Actors;

namespace Messaging.Application.OutgoingMessages;

/// <summary>
/// Service for looking up actor details
/// </summary>
public interface IActorLookup
{
    /// <summary>
    /// Get actor unique id by actor number
    /// </summary>
    /// <param name="actorNumber"></param>
    Task<Guid> GetIdByActorNumberAsync(string actorNumber);

    /// <summary>
    /// Get actor number by id
    /// </summary>
    /// <param name="actorId"></param>
    Task<string> GetActorNumberByIdAsync(Guid actorId);

    /// <summary>
    /// Get actor number by id
    /// </summary>
    /// <param name="actorId"></param>
    Task<ActorNumber?> GetActorNumberByB2CIdAsync(Guid actorId);
}
