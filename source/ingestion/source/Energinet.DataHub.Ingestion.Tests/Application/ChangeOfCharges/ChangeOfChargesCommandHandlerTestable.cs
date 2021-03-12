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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.Repositories;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using GreenEnergyHub.Iso8601;
using GreenEnergyHub.Messaging;
using GreenEnergyHub.Queues.ValidationReportDispatcher;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.Ingestion.Tests.Application.ChangeOfCharges
{
    public class ChangeOfChargesCommandHandlerTestable : ChangeOfChargesCommandHandler
    {
        public ChangeOfChargesCommandHandlerTestable(
            ILogger<ChangeOfChargesCommandHandler> logger,
            IValidationReportQueueDispatcher validationReportQueueDispatcher,
            IChargeRepository chargeRepository,
            IIso8601Durations iso8601Durations,
            IEnergySupplierNotifier energySupplierNotifier,
            IChangeOfChargesDomainValidator changeOfDomainValidator,
            IChangeOfChargesInputValidator inputValidator)
            : base(logger, validationReportQueueDispatcher, chargeRepository, iso8601Durations, energySupplierNotifier, changeOfDomainValidator, inputValidator)
        {
        }

        public async Task<bool> CallValidateAsync(ChangeOfChargesMessage changeOfChargesMessage)
        {
            return await ValidateAsync(changeOfChargesMessage, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task CallAcceptAsync(ChangeOfChargesMessage changeOfChargesMessage)
        {
            await AcceptAsync(changeOfChargesMessage, CancellationToken.None);
        }

        public async Task CallRejectAsync(ChangeOfChargesMessage changeOfChargesMessage)
        {
            await RejectAsync(changeOfChargesMessage, CancellationToken.None);
        }
    }
}
