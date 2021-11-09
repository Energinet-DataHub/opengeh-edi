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
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Application.Connected
{
    public class MeteringPointConnectedHandler : INotificationHandler<MeteringPointConnected>
    {
        private readonly IAccountingPointRepository _accountingPointRepository;

        public MeteringPointConnectedHandler(IAccountingPointRepository accountingPointRepository)
        {
            _accountingPointRepository = accountingPointRepository;
        }

        public async Task Handle(MeteringPointConnected notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            var accountingPoint = await _accountingPointRepository.GetByIdAsync(
                AccountingPointId.Create(Guid.Parse(notification.MeteringPointId))).ConfigureAwait(false);
            accountingPoint.Connect();
        }
    }
}
