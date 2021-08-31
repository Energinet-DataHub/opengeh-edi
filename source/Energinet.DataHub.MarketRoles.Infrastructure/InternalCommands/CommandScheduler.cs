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
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands
{
    public class CommandScheduler : ICommandScheduler
    {
        private readonly MarketRolesContext _context;
        private readonly MessageSerializer _serializer;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly ICorrelationContext _correlationContext;

        public CommandScheduler(MarketRolesContext context, MessageSerializer serializer, ISystemDateTimeProvider systemDateTimeProvider, ICorrelationContext correlationContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
        }

        public async Task EnqueueAsync<TCommand>(TCommand command, BusinessProcessId businessProcessId, Instant? scheduleDate)
            where TCommand : InternalCommand
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (businessProcessId == null) throw new ArgumentNullException(nameof(businessProcessId));

            var data = await _serializer.ToBytesAsync(command, CancellationToken.None).ConfigureAwait(false);
            var type = command.GetType().FullName;
            var queuedCommand = new QueuedInternalCommand(command.Id, type!, data, _systemDateTimeProvider.Now(), businessProcessId.Value, scheduleDate!, _correlationContext.Id);
            await _context.QueuedInternalCommands.AddAsync(queuedCommand).ConfigureAwait(false);
        }
    }
}
