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
using Energinet.DataHub.MarketRoles.Application.Common;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing.Pipeline
{
    public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBusinessRequest
        where TResponse : BusinessProcessResult
    {
        private readonly IBusinessProcessResponder<TRequest> _businessProcessResponder;

        public AuthorizationBehaviour(IBusinessProcessResponder<TRequest> businessProcessResponder)
        {
            _businessProcessResponder = businessProcessResponder ?? throw new ArgumentNullException(nameof(businessProcessResponder));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (next == null) throw new ArgumentNullException(nameof(next));
            // TODO: Invoke authorization handlers/service
            bool success = true;
            if (success)
            {
                return await next().ConfigureAwait(false);
            }
            else
            {
                var result = (TResponse)BusinessProcessResult.Fail(request.TransactionId);
                await _businessProcessResponder.RespondAsync(request, result).ConfigureAwait(false);
                return result;
            }
        }
    }
}
