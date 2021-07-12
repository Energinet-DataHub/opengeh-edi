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
using Energinet.DataHub.MarketRoles.Application;
using Energinet.DataHub.MarketRoles.Application.Common.DomainEvents;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DomainEventDispatching
{
    public class DomainEventPublisher : IDomainEventPublisher
    {
        private readonly IMediator _mediator;

        public DomainEventPublisher(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public Task PublishAsync(IDomainEvent domainEvent)
        {
            return _mediator.Publish(domainEvent);
        }
    }
}
