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
using Application.Configuration.Commands.Commands;
using Domain.Actors;
using Domain.OutgoingMessages.Queueing;
using MediatR;

namespace Application.OutgoingMessages.Dequeue;

public class DequeueHandler : IRequestHandler<DequeueCommand, DequeCommandResult>
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;

    public DequeueHandler(IActorMessageQueueRepository actorMessageQueueRepository)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
    }

    public async Task<DequeCommandResult> Handle(DequeueCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bundleId = BundleId.Create(request.MessageId);
        var actorQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(request.ActorNumber, request.MarketRole).ConfigureAwait(false);

        if (actorQueue == null)
        {
            return new DequeCommandResult(false);
        }

        actorQueue.Dequeue(bundleId);

        return new DequeCommandResult(true);
    }
}

public record DequeueCommand(Guid MessageId, MarketRole MarketRole, ActorNumber ActorNumber) : ICommand<DequeCommandResult>;

public record DequeCommandResult(bool Success);
