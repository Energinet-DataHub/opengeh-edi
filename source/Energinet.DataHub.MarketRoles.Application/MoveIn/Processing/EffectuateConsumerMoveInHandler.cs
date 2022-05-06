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
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Application.MoveIn.Processing
{
    public class EffectuateConsumerMoveInHandler : ICommandHandler<EffectuateConsumerMoveIn>
    {
        private readonly IAccountingPointRepository _accountingPointRepository;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public EffectuateConsumerMoveInHandler(IAccountingPointRepository accountingPointRepository, ISystemDateTimeProvider systemDateTimeProvider)
        {
            _accountingPointRepository = accountingPointRepository ?? throw new ArgumentNullException(nameof(accountingPointRepository));
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
        }

        public async Task<Unit> Handle(EffectuateConsumerMoveIn request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var accountingPoint = await _accountingPointRepository.GetByIdAsync(AccountingPointId.Create(request.AccountingPointId)).ConfigureAwait(false);
            accountingPoint?.EffectuateConsumerMoveIn(Transaction.Create(request.Transaction), _systemDateTimeProvider);
            return Unit.Value;
        }
    }
}
