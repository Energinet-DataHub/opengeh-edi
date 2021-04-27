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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using GreenEnergyHub.Messaging;
using MediatR;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier
{
    public class RequestChangeSupplierCommandHandler : IRequestHandler<RequestChangeOfSupplier, RequestChangeOfSupplierResult>
    {
        private readonly IRuleEngine<RequestChangeOfSupplier> _ruleEngine;
        private readonly IMeteringPointRepository _meteringPointRepository;
        private readonly ISystemDateTimeProvider _systemTimeProvider;
        private readonly IEnergySupplierRepository _energySupplierRepository;

        public RequestChangeSupplierCommandHandler(
            IRuleEngine<RequestChangeOfSupplier> ruleEngine,
            IMeteringPointRepository meteringPointRepository,
            ISystemDateTimeProvider systemTimeProvider,
            IEnergySupplierRepository energySupplierRepository)
        {
            _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
            _meteringPointRepository = meteringPointRepository ?? throw new ArgumentNullException(nameof(meteringPointRepository));
            _systemTimeProvider = systemTimeProvider ?? throw new ArgumentNullException(nameof(systemTimeProvider));
            _energySupplierRepository = energySupplierRepository ?? throw new ArgumentNullException(nameof(energySupplierRepository));
        }

        public async Task<RequestChangeOfSupplierResult> Handle(RequestChangeOfSupplier command, CancellationToken cancellationToken)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var inputValidationResult = await RunInputValidationsAsync(command).ConfigureAwait(false);
            if (!inputValidationResult.Succeeded)
            {
                return inputValidationResult;
            }

            var preCheckResult = await RunPreChecksAsync(command).ConfigureAwait(false);
            if (!preCheckResult.Succeeded)
            {
                return preCheckResult;
            }

            var meteringPoint = await GetMeteringPointAsync(command.MarketEvaluationPoint.MRid).ConfigureAwait(false);
            if (meteringPoint == null)
            {
                return RequestChangeOfSupplierResult.Reject("MeteringPointNotFound");
            }

            var rulesCheckResult = CheckBusinessRules(command, meteringPoint);
            if (!rulesCheckResult.Succeeded)
            {
                return rulesCheckResult;
            }

            meteringPoint.AcceptChangeOfSupplier(new EnergySupplierId(command.EnergySupplier.MRID!), command.StartDate, new ProcessId(command.Transaction.MRID), _systemTimeProvider);
            await _meteringPointRepository.SaveAsync(meteringPoint);

            return RequestChangeOfSupplierResult.Success();
        }

        private async Task<RequestChangeOfSupplierResult> RunInputValidationsAsync(RequestChangeOfSupplier command)
        {
            var result = await _ruleEngine.ValidateAsync(command).ConfigureAwait(false);
            if (result.Success)
            {
                return RequestChangeOfSupplierResult.Success();
            }

            var errors = result.Select(error => error.RuleNumber).ToList();
            return RequestChangeOfSupplierResult.Reject(errors);
        }

        private async Task<RequestChangeOfSupplierResult> RunPreChecksAsync(RequestChangeOfSupplier request)
        {
           var preCheckResults = new List<string>();

           if (await EnergySupplierIsUnknownAsync(request.EnergySupplier.MRID!).ConfigureAwait(false))
           {
               preCheckResults.Add("EnergySupplierDoesNotExist");
           }

           return preCheckResults.Count > 0
               ? RequestChangeOfSupplierResult.Reject(preCheckResults)
               : RequestChangeOfSupplierResult.Success();
        }

        private RequestChangeOfSupplierResult CheckBusinessRules(RequestChangeOfSupplier command, AccountingPoint accountingPoint)
        {
            var validationResult =
                accountingPoint.ChangeSupplierAcceptable(new EnergySupplierId(command.EnergySupplier.MRID!), command.StartDate, _systemTimeProvider);

            return validationResult.Success
                ? RequestChangeOfSupplierResult.Reject(validationResult.Errors)
                : RequestChangeOfSupplierResult.Success();
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
