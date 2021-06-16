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
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Application.MoveIn
{
    public class RequestMoveInHandler : IBusinessRequestHandler<RequestMoveIn>
    {
        private readonly IAccountingPointRepository _accountingPointRepository;
        private readonly IEnergySupplierRepository _energySupplierRepository;
        private readonly IConsumerRepository _consumerRepository;

        public RequestMoveInHandler(IAccountingPointRepository accountingPointRepository, IEnergySupplierRepository energySupplierRepository, IConsumerRepository consumerRepository)
        {
            _accountingPointRepository = accountingPointRepository ?? throw new ArgumentNullException(nameof(accountingPointRepository));
            _energySupplierRepository = energySupplierRepository ?? throw new ArgumentNullException(nameof(energySupplierRepository));
            _consumerRepository = consumerRepository ?? throw new ArgumentNullException(nameof(consumerRepository));
        }

        public async Task<BusinessProcessResult> Handle(RequestMoveIn request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var energySupplier = await _energySupplierRepository.GetByGlnNumberAsync(new GlnNumber(request.EnergySupplierGlnNumber)).ConfigureAwait(false);
            var accountingPoint = await _accountingPointRepository.GetByGsrnNumberAsync(GsrnNumber.Create(request.AccountingPointGsrnNumber)).ConfigureAwait(false);

            var validationResult = CheckRules(energySupplier, accountingPoint, request);
            if (validationResult.Success == false)
            {
                return validationResult;
            }

            //TODO: Add logic for creating correct type of consumer (SSN or VAT). Also check if consumer already exists
            var consumer = new Consumer(ConsumerId.New(), new CprNumber(request.VATNumber));
            _consumerRepository.Add(consumer);

            accountingPoint.AcceptConsumerMoveIn(consumer.ConsumerId, energySupplier.EnergySupplierId, request.MoveInDate, Transaction.Create(request.TransactionId));
            return BusinessProcessResult.Ok(request.TransactionId);
        }

        private static BusinessProcessResult CheckRules(EnergySupplier energySupplier, AccountingPoint accountingPoint, RequestMoveIn request)
        {
            var rules = new List<IBusinessRule>()
            {
                new EnergySupplierMustBeKnownRule(energySupplier, request.EnergySupplierGlnNumber),
                new MeteringPointMustBeKnownRule(accountingPoint, request.AccountingPointGsrnNumber),
            };

            return new BusinessProcessResult(request.TransactionId, rules);
        }
    }
}
