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
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;

namespace Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands
{
    public class InternalCommandProcessor : IInternalCommandProcessor
    {
        private readonly IInternalCommandAccessor _internalCommandAccessor;
        private readonly IInternalCommandDispatcher _internalCommandDispatcher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public InternalCommandProcessor(IInternalCommandAccessor internalCommandAccessor, IInternalCommandDispatcher internalCommandDispatcher, IUnitOfWork unitOfWork, ISystemDateTimeProvider systemDateTimeProvider)
        {
            _internalCommandAccessor = internalCommandAccessor ?? throw new ArgumentNullException(nameof(internalCommandAccessor));
            _internalCommandDispatcher = internalCommandDispatcher ?? throw new ArgumentNullException(nameof(internalCommandDispatcher));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
        }

        public async Task ProcessUndispatchedAsync()
        {
            var undispatchedCommands = await _internalCommandAccessor.GetUndispatchedAsync().ConfigureAwait(false);

            foreach (var queuedCommand in undispatchedCommands)
            {
                var dispatchResult = await _internalCommandDispatcher.DispatchAsync(queuedCommand).ConfigureAwait(false);
                queuedCommand.SetDispatched(_systemDateTimeProvider.Now());

                if (dispatchResult.SequenceId != default(long))
                {
                    queuedCommand.SetSequenceId(dispatchResult.SequenceId);
                }

                await _unitOfWork.CommitAsync().ConfigureAwait(false);
            }
        }
    }
}
