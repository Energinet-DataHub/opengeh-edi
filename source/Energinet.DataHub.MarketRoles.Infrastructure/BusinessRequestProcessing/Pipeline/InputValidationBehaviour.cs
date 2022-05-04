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
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using FluentValidation;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing.Pipeline
{
    public class InputValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBusinessRequest, MediatR.IRequest<TResponse>
        where TResponse : BusinessProcessResult
    {
        private readonly IValidator<TRequest> _validator;
        private readonly IBusinessProcessResultHandler<TRequest> _businessProcessResultHandler;

        public InputValidationBehaviour(IValidator<TRequest> validator, IBusinessProcessResultHandler<TRequest> businessProcessResultHandler)
        {
            _validator = validator;
            _businessProcessResultHandler = businessProcessResultHandler ?? throw new ArgumentNullException(nameof(businessProcessResultHandler));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (next == null) throw new ArgumentNullException(nameof(next));

            var validationResult = await _validator.ValidateAsync(request).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult
                    .Errors
                    .Select(error => (ValidationError)error.CustomState)
                    .ToList()
                    .AsReadOnly();

                var result = new BusinessProcessResult(request.TransactionId, validationErrors);
                await _businessProcessResultHandler.HandleAsync(request, result).ConfigureAwait(false);
                return (TResponse)result;
            }

            return await next().ConfigureAwait(false);
        }
    }
}
