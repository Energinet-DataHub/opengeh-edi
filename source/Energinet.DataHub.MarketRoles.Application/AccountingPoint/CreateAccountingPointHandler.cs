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

namespace Energinet.DataHub.MarketRoles.Application.AccountingPoint
{
    public class CreateAccountingPointHandler : ICommandHandler<CreateAccountingPoint>
    {
        private readonly IAccountingPointRepository _accountingPointRepository;

        public CreateAccountingPointHandler(IAccountingPointRepository accountingPointRepository)
        {
            _accountingPointRepository = accountingPointRepository;
        }

        public Task<Unit> Handle(CreateAccountingPoint request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var accountingPoint = new Domain.MeteringPoints.AccountingPoint(
                AccountingPointId.Create(Guid.Parse(request.AccountingPointId)),
                GsrnNumber.Create(request.GsrnNumber),
                EnumerationType.FromName<MeteringPointType>(request.MeteringPointType),
                EnumerationType.FromName<PhysicalState>(request.PhysicalState));

            _accountingPointRepository.Add(accountingPoint);
            return Task.FromResult(Unit.Value);
        }
    }
}
