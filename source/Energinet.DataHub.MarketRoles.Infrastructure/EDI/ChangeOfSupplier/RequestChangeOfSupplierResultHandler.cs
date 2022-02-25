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
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.EDI;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier
{
    public class RequestChangeOfSupplierResultHandler : IBusinessProcessResultHandler<RequestChangeOfSupplier>
    {
        private readonly IActorMessageService _actorMessageService;
        private readonly ErrorMessageFactory _errorMessageFactory;

        public RequestChangeOfSupplierResultHandler(
            IActorMessageService actorMessageService,
            ErrorMessageFactory errorMessageFactory)
        {
            _actorMessageService = actorMessageService;
            _errorMessageFactory = errorMessageFactory;
        }

        public Task HandleAsync(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            return result.Success
                ? CreateAcceptResponseAsync(request)
                : CreateRejectResponseAsync(request, result);
        }

        private async Task CreateAcceptResponseAsync(RequestChangeOfSupplier request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            await _actorMessageService.SendChangeOfSupplierConfirmAsync(
                    request.TransactionId,
                    request.AccountingPointGsrnNumber)
                .ConfigureAwait(false);
        }

        private async Task CreateRejectResponseAsync(RequestChangeOfSupplier request, BusinessProcessResult result)
        {
            var errors = result.ValidationErrors
                .Select(error => _errorMessageFactory.GetErrorMessage(error))
                .AsEnumerable();

            await _actorMessageService
                .SendChangeOfSupplierRejectAsync(request.TransactionId, request.AccountingPointGsrnNumber, errors)
                .ConfigureAwait(false);
        }
    }
}
