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

using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;
using MediatR;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.IntegrationEvents;

public class TestIntegrationEventMapper : IIntegrationEventMapper
{
    public string EventTypeToHandle => TestIntegrationEventMessage.TestIntegrationEventName;

    public int MappedCount { get; private set; }

    public Task<ICommand<Unit>> MapToCommandAsync(IntegrationEvent integrationEvent)
    {
        MappedCount++;

        return Task.FromResult<ICommand<Unit>>(new TestCommand());
    }
}
