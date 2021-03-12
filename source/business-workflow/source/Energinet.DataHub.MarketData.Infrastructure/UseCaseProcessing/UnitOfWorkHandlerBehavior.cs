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
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using MediatR;

namespace Energinet.DataHub.MarketData.Infrastructure.UseCaseProcessing
{
    public class UnitOfWorkHandlerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IUnitOfWorkCallback _unitOfWorkCallback;

        public UnitOfWorkHandlerBehavior(IUnitOfWorkCallback unitOfWorkCallback)
        {
            _unitOfWorkCallback = unitOfWorkCallback ?? throw new ArgumentNullException(nameof(unitOfWorkCallback));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var result = await next().ConfigureAwait(false);
            await _unitOfWorkCallback.CommitAsync().ConfigureAwait(false);
            return result;
        }
    }
}
