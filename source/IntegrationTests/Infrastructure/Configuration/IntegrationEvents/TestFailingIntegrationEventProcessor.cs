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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;
using MediatR;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.IntegrationEvents;

public class TestFailingIntegrationEventProcessor : IIntegrationEventProcessor
{
    private readonly IMediator _mediator;

    public TestFailingIntegrationEventProcessor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public string EventTypeToHandle => TestIntegrationEventMessage.TestIntegrationEventName;

    public int MappedCount { get; private set; }

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        MappedCount++;

        var fromResult = await Task.FromResult<ICommand<Unit>>(new TestCommand());

        await _mediator.Send(fromResult, cancellationToken).ConfigureAwait(false);

#pragma warning disable CA2201
        throw new Exception("Test exception");
#pragma warning restore CA2201
    }
}
