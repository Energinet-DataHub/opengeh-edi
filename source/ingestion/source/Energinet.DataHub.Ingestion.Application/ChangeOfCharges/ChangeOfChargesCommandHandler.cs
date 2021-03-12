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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.Repositories;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using GreenEnergyHub.Iso8601;
using GreenEnergyHub.Messaging.Dispatching;
using GreenEnergyHub.Messaging.Validation.Exceptions;
using GreenEnergyHub.Queues.ValidationReportDispatcher;
using GreenEnergyHub.Queues.ValidationReportDispatcher.Validation;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.Ingestion.Application.ChangeOfCharges
{
    /// <summary>
    /// Class which defines how to handle <see cref="ChangeOfChargesMessage"/>.
    /// </summary>
    public class ChangeOfChargesCommandHandler : HubCommandHandler<ChangeOfChargesMessage>
    {
        private readonly ILogger _logger;
        private readonly IValidationReportQueueDispatcher _validationReportQueueDispatcher;
        private readonly IChargeRepository _chargeRepository;
        private readonly IIso8601Durations _iso8601Durations;
        private readonly IEnergySupplierNotifier _energySupplierNotifier;
        private readonly IChangeOfChargesDomainValidator _domainValidator;
        private readonly IChangeOfChargesInputValidator _inputValidator;

        private HubRequestValidationResult? _validationResult;

        public ChangeOfChargesCommandHandler(
            ILogger<ChangeOfChargesCommandHandler> logger,
            IValidationReportQueueDispatcher validationReportQueueDispatcher,
            IChargeRepository chargeRepository,
            IIso8601Durations iso8601Durations,
            IEnergySupplierNotifier energySupplierNotifier,
            IChangeOfChargesDomainValidator domainValidator,
            IChangeOfChargesInputValidator inputValidator)
        {
            _logger = logger;
            _validationReportQueueDispatcher = validationReportQueueDispatcher;
            _chargeRepository = chargeRepository;
            _iso8601Durations = iso8601Durations;
            _energySupplierNotifier = energySupplierNotifier;
            _domainValidator = domainValidator;
            _inputValidator = inputValidator;
        }

        protected override async Task AcceptAsync(ChangeOfChargesMessage actionData, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(ChangeOfChargesMessage)} have parsed validation");

            foreach (var point in actionData.Period!.Points!)
            {
                var time = _iso8601Durations.AddDuration(actionData.MktActivityRecord!.ValidityStartDate, actionData.Period.Resolution, point.Position);
                point.Time = time;
            }

            actionData.CorrelationId = Guid.NewGuid().ToString();
            actionData.LastUpdatedBy = "someone";

            var result = await _chargeRepository.StoreChargeAsync(actionData).ConfigureAwait(false);
            if (!result.Success) return;

            await _energySupplierNotifier.NotifyAboutChangeOfChargesAsync(actionData);
        }

        protected override Task OnErrorAsync(Exception innerException)
        {
            if (innerException is ValidationPropertyNullException)
            {
                // TODO: Decide on strategy: 1) Message is stranded here (should be theoretical scenario) - what to do with it? 2) What to send to validation report queue. AzDO story: https://dev.azure.com/energinet/Datahub/_backlogs/backlog/Volt/Stories/?workitem=115506
                _logger.LogError($"{nameof(innerException)} thrown due to {nameof(ValidationPropertyNullException)} thrown by input validation.");
                return Task.CompletedTask;
            }

            throw innerException;
        }

        protected override async Task RejectAsync(ChangeOfChargesMessage actionData, CancellationToken cancellationToken)
        {
            await _validationReportQueueDispatcher.DispatchAsync(_validationResult).ConfigureAwait(false);
        }

        protected override async Task<bool> ValidateAsync(ChangeOfChargesMessage changeOfChargesMessage, CancellationToken cancellationToken)
        {
            _validationResult = await _inputValidator.ValidateAsync(changeOfChargesMessage);
            if (!_validationResult.Success) return false;

            _validationResult = await _domainValidator.ValidateAsync(changeOfChargesMessage);
            return !_validationResult.Errors.Any();
        }
    }
}
