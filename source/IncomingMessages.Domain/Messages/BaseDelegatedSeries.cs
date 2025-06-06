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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;

public abstract record BaseDelegatedSeries : IDelegatedIncomingMessageSeries
{
    public bool IsDelegated { get; private set; }

    public ActorNumber? OriginalActorNumber { get; private set; }

    public ActorRole? RequestedByActorRole { get; private set; }

    public IReadOnlyCollection<string> DelegatedGridAreas { get; private set; } = Array.Empty<string>();

    public void DelegateSeries(ActorNumber? originalActorNumber, ActorRole requestedByActorRole, IReadOnlyCollection<string> delegatedGridAreas)
    {
        IsDelegated = true;
        OriginalActorNumber = originalActorNumber;
        RequestedByActorRole = requestedByActorRole;
        DelegatedGridAreas = delegatedGridAreas;
    }
}
