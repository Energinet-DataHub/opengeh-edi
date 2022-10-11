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
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Commands;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.Serialization;
using NodaTime;

namespace Messaging.Infrastructure.Configuration.InternalCommands
{
    public class CommandScheduler : ICommandScheduler
    {
        private readonly InternalCommandMapper _internalCommandMapper;
        private readonly B2BContext _context;
        private readonly ISerializer _serializer;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public CommandScheduler(
            InternalCommandMapper internalCommandMapper,
            B2BContext context,
            ISerializer serializer,
            ISystemDateTimeProvider systemDateTimeProvider)
        {
            _internalCommandMapper = internalCommandMapper;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _systemDateTimeProvider =
                systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
        }

        public async Task EnqueueAsync<TCommand>(TCommand command)
            where TCommand : InternalCommand
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var data = _serializer.Serialize(command);
            var commandMetadata = _internalCommandMapper.GetByType(command.GetType());
            var queuedCommand = new QueuedInternalCommand(command.Id, commandMetadata.CommandName, data, _systemDateTimeProvider.Now());
            await _context.QueuedInternalCommands.AddAsync(queuedCommand).ConfigureAwait(false);
        }
    }
}
