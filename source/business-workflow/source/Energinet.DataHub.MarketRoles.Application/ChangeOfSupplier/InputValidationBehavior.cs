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
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier
{
    public class InputValidationBehavior : IPipelineBehavior<RequestChangeOfSupplier, RequestChangeOfSupplierResult>
    {
        private readonly ILogger _logger;

        public InputValidationBehavior(
            ILogger logger)
        {
            _logger = logger;
        }

        public async Task<RequestChangeOfSupplierResult> Handle(RequestChangeOfSupplier request, CancellationToken cancellationToken, RequestHandlerDelegate<RequestChangeOfSupplierResult> next)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (next == null) throw new ArgumentNullException(nameof(next));

            _logger.LogInformation("Validated: {request}", request.Transaction);

            var result = await next().ConfigureAwait(false);
            if (result == null)
            {
                return new RequestChangeOfSupplierResult();
            }

            return result;
        }
    }
}
