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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;

/// <summary>
/// Contains the current authorized identity
/// </summary>
public class AuthenticatedActor
{
    private ActorIdentity? _currentIdentity;

    /// <summary>
    /// Current identity
    /// </summary>
    public ActorIdentity CurrentActorIdentity
    {
        get
        {
            if (_currentIdentity is null) throw new InvalidOperationException("Current identity is not set");
            return _currentIdentity;
        }
    }

    /// <summary>
    /// Try get Current identity
    /// </summary>
    public bool TryGetCurrentActorIdentity(out ActorIdentity? actorIdentity)
    {
        actorIdentity = _currentIdentity;
        return actorIdentity != null;
    }

    /// <summary>
    /// Set the authenticated actor
    /// </summary>
    public void SetAuthenticatedActor(ActorIdentity? actorIdentity)
    {
        _currentIdentity = actorIdentity;
    }
}
