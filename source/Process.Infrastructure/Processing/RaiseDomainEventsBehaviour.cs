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

using MediatR;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Processing;

public class RaiseDomainEventsBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMediator _mediator;
    private readonly DomainEventsAccessor _domainEventsAccessor;

    public RaiseDomainEventsBehaviour(IMediator mediator, DomainEventsAccessor domainEventsAccessor)
    {
        _mediator = mediator;
        _domainEventsAccessor = domainEventsAccessor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var result = await next().ConfigureAwait(false);
        var domainEvents = _domainEventsAccessor.GetAllDomainEvents();
        foreach (var domainEvent in domainEvents)
        {
            _domainEventsAccessor.ClearAllDomainEvent(domainEvent);
            await _mediator.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }

        _domainEventsAccessor.ClearAllDomainEvents();
        return result;
    }
}
