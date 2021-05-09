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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.Events;
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Application.Common.DomainEvents;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.Commands.MeteringPointDetails
{
    public class ForwardMeteringPointDetailsHandler : ICommandHandler<ForwardMeteringPointDetails>
    {
        private readonly IDomainEventPublisher _domainEventPublisher;
        private readonly IMeteringPointDetailsForwarder _meteringPointDetailsForwarder;

        public ForwardMeteringPointDetailsHandler(IDomainEventPublisher domainEventPublisher, IMeteringPointDetailsForwarder meteringPointDetailsForwarder)
        {
            _domainEventPublisher = domainEventPublisher ?? throw new ArgumentNullException(nameof(domainEventPublisher));
            _meteringPointDetailsForwarder = meteringPointDetailsForwarder ?? throw new ArgumentNullException(nameof(meteringPointDetailsForwarder));
        }

        public async Task<Unit> Handle(ForwardMeteringPointDetails request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            await _meteringPointDetailsForwarder.ForwardAsync(AccountingPointId.Create(request.AccountingPointId)).ConfigureAwait(false);

            await _domainEventPublisher.PublishAsync(new MeteringPointDetailsDispatched(
                AccountingPointId.Create(request.AccountingPointId),
                BusinessProcessId.Create(request.BusinessProcessId),
                Transaction.Create(request.Transaction))).ConfigureAwait(false);

            return Unit.Value;
        }
    }
}
