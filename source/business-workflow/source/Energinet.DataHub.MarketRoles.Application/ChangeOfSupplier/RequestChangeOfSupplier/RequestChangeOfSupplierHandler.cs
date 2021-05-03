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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.RequestChangeOfSupplier.Validation;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.RequestChangeOfSupplier
{
    public class RequestChangeOfSupplierHandler : IBusinessRequestHandler<RequestChangeOfSupplier>
    {
        private readonly IMeteringPointRepository _meteringPointRepository;
        private readonly ISystemDateTimeProvider _systemTimeProvider;
        private readonly IEnergySupplierRepository _energySupplierRepository;

        public RequestChangeOfSupplierHandler(
            IMeteringPointRepository meteringPointRepository,
            ISystemDateTimeProvider systemTimeProvider,
            IEnergySupplierRepository energySupplierRepository)
        {
            _meteringPointRepository = meteringPointRepository ?? throw new ArgumentNullException(nameof(meteringPointRepository));
            _systemTimeProvider = systemTimeProvider ?? throw new ArgumentNullException(nameof(systemTimeProvider));
            _energySupplierRepository = energySupplierRepository ?? throw new ArgumentNullException(nameof(energySupplierRepository));
        }

        public async Task<BusinessProcessResult> Handle(RequestChangeOfSupplier request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var preCheckResult = await RunPreChecksAsync(request).ConfigureAwait(false);
            if (!preCheckResult.Success)
            {
                return preCheckResult;
            }

            var meteringPoint = await GetMeteringPointAsync(request.MeteringPointId).ConfigureAwait(false);
            if (meteringPoint == null)
            {
                return new BusinessProcessResult(request.TransactionId, new UnknownMeteringPoint(true, request.MeteringPointId));
            }

            var rulesCheckResult = CheckBusinessRules(request, meteringPoint);
            if (!rulesCheckResult.Success)
            {
                return rulesCheckResult;
            }

            meteringPoint.AcceptChangeOfSupplier(new EnergySupplierId(request.EnergySupplierId), request.StartDate, new ProcessId(request.TransactionId), _systemTimeProvider);

            return BusinessProcessResult.Ok(request.TransactionId);
        }

        private async Task<BusinessProcessResult> RunPreChecksAsync(RequestChangeOfSupplier request)
        {
            var validationRules = new List<IBusinessRule>();
            if (await EnergySupplierIsUnknownAsync(request.EnergySupplierId).ConfigureAwait(false))
            {
               validationRules.Add(new UnknownEnergySupplierRule(true, request.EnergySupplierId));
            }

            return new BusinessProcessResult(request.TransactionId, validationRules);
        }

        private BusinessProcessResult CheckBusinessRules(RequestChangeOfSupplier request, AccountingPoint accountingPoint)
        {
            var validationResult =
                accountingPoint.ChangeSupplierAcceptable(new EnergySupplierId(request.EnergySupplierId), request.StartDate, _systemTimeProvider);

            return new BusinessProcessResult(request.TransactionId, validationResult.Errors);
        }

        private async Task<bool> EnergySupplierIsUnknownAsync(string energySupplierId)
        {
            return !await _energySupplierRepository.ExistsAsync(new GlnNumber(energySupplierId)).ConfigureAwait(false);
        }

        private Task<AccountingPoint> GetMeteringPointAsync(string gsrnNumber)
        {
            var meteringPointId = GsrnNumber.Create(gsrnNumber);
            var meteringPoint =
                _meteringPointRepository.GetByGsrnNumberAsync(meteringPointId);
            return meteringPoint;
        }
    }
}
