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
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.Application.Configuration.Commands.Commands;
using Energinet.DataHub.EDI.Application.GridAreas;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;

public class GridAreaOwnershipAssignedMapper : IIntegrationEventMapper
{
    public string EventTypeToHandle => GridAreaOwnershipAssigned.EventName;

    public Task<InternalCommand> MapToCommandAsync(IntegrationEvent integrationEvent)
    {
        if (integrationEvent == null)
            throw new ArgumentNullException(nameof(integrationEvent));

        var gridAreaOwnershipAssignedEvent = (GridAreaOwnershipAssigned)integrationEvent.Message;

        return Task.FromResult<InternalCommand>(new GridAreaOwnershipAssignedCommand(gridAreaOwnershipAssignedEvent.GridAreaCode, gridAreaOwnershipAssignedEvent.ValidFrom.ToInstant(), ActorNumber.Create(gridAreaOwnershipAssignedEvent.ActorNumber)));
    }
}
