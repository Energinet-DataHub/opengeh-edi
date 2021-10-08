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
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.Common.Validation;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime.Text;

namespace Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier
{
    public class RequestChangeOfSupplierHandler : IBusinessRequestHandler<RequestChangeOfSupplier>
    {
        private readonly IAccountingPointRepository _accountingPointRepository;
        private readonly ISystemDateTimeProvider _systemTimeProvider;
        private readonly IEnergySupplierRepository _energySupplierRepository;
        private EnergySupplier? _energySupplier;
        private Domain.MeteringPoints.AccountingPoint? _accountingPoint;
        private RequestChangeOfSupplier? _request;

        public RequestChangeOfSupplierHandler(
            IAccountingPointRepository accountingPointRepository,
            ISystemDateTimeProvider systemTimeProvider,
            IEnergySupplierRepository energySupplierRepository)
        {
            _accountingPointRepository = accountingPointRepository ?? throw new ArgumentNullException(nameof(accountingPointRepository));
            _systemTimeProvider = systemTimeProvider ?? throw new ArgumentNullException(nameof(systemTimeProvider));
            _energySupplierRepository = energySupplierRepository ?? throw new ArgumentNullException(nameof(energySupplierRepository));
        }

        public async Task<BusinessProcessResult> Handle(RequestChangeOfSupplier request, CancellationToken cancellationToken)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _energySupplier = await _energySupplierRepository.GetByGlnNumberAsync(new GlnNumber(request.EnergySupplierGlnNumber)).ConfigureAwait(false);
            _accountingPoint = await GetMeteringPointAsync(request.AccountingPointGsrnNumber).ConfigureAwait(false);

            var validationResult = Validate();
            if (!validationResult.Success)
            {
                return validationResult;
            }

            var rulesCheckResult = CheckBusinessRules();
            if (!rulesCheckResult.Success)
            {
                return rulesCheckResult;
            }

            var startDate = InstantPattern.General.Parse(request.StartDate).Value;

            _accountingPoint.AcceptChangeOfSupplier(_energySupplier.EnergySupplierId, startDate, new Transaction(request.TransactionId), _systemTimeProvider);

            return BusinessProcessResult.Ok(request.TransactionId);
        }

        private BusinessProcessResult Validate()
        {
            if (_request is null) throw new ArgumentNullException(nameof(_request));

            var validationRules = new List<IBusinessRule>()
            {
                new EnergySupplierMustBeKnownRule(_energySupplier, _request.EnergySupplierGlnNumber),
                new MeteringPointMustBeKnownRule(_accountingPoint, _request.AccountingPointGsrnNumber),
            };

            return new BusinessProcessResult(_request.TransactionId, validationRules);
        }

        private BusinessProcessResult CheckBusinessRules()
        {
            if (_request is null) throw new ArgumentNullException(nameof(_request));

            var startDate = InstantPattern.General.Parse(_request.StartDate).Value;

            var validationResult =
                _accountingPoint!.ChangeSupplierAcceptable(_energySupplier!.EnergySupplierId, startDate, _systemTimeProvider);

            return new BusinessProcessResult(_request.TransactionId, validationResult.Errors);
        }

        private Task<Domain.MeteringPoints.AccountingPoint> GetMeteringPointAsync(string gsrnNumber)
        {
            var meteringPointId = GsrnNumber.Create(gsrnNumber);
            var meteringPoint =
                _accountingPointRepository.GetByGsrnNumberAsync(meteringPointId);
            return meteringPoint;
        }
    }
}
