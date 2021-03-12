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
using Energinet.DataHub.MarketData.Domain.SeedWork;
using MediatR;

namespace Energinet.DataHub.MarketData.Infrastructure.UseCaseProcessing
{
    /// <summary>
    /// When a request is done being processed,
    /// all pending domain events are sent to any handlers that listen on the event
    /// <remarks>If an exception is raised and reaches the behavior, then no events are published</remarks>
    /// </summary>
    public class DomainEventPublisherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly DomainEventsContext _eventsContext;
        private readonly IMediator _mediator;

        public DomainEventPublisherBehavior(DomainEventsContext eventsContext, IMediator mediator)
        {
            _eventsContext = eventsContext ?? throw new ArgumentNullException(nameof(eventsContext));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (next == null) throw new ArgumentNullException(nameof(next));

            var pipelineResponse = await next();

            await _eventsContext.PublishEventsAsync(_mediator.Publish, cancellationToken).ConfigureAwait(false);

            return pipelineResponse;
        }
    }
}
