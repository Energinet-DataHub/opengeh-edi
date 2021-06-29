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
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using JetBrains.Annotations;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing.Pipeline
{
    public class UnitOfWorkBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly MarketRolesContext _context;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public UnitOfWorkBehaviour(IUnitOfWork unitOfWork, MarketRolesContext context, ISystemDateTimeProvider systemDateTimeProvider)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            var result = await next().ConfigureAwait(false);

            if (request is InternalCommand command)
            {
                var queuedCommand = await _context.QueuedInternalCommands.FindAsync(command.Id).ConfigureAwait(false);
                queuedCommand?.SetProcessed(_systemDateTimeProvider.Now());
            }

            await _unitOfWork.CommitAsync().ConfigureAwait(false);

            return result;
        }
    }
}
