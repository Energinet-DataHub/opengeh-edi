﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.Process.Domain.Commands;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using NodaTime;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;

public class CommandScheduler : ICommandScheduler
{
    private readonly InternalCommandMapper _internalCommandMapper;
    private readonly ProcessContext _context;
    private readonly ISerializer _serializer;
    private readonly IClock _clock;

    public CommandScheduler(
        InternalCommandMapper internalCommandMapper,
        ProcessContext context,
        ISerializer serializer,
        IClock clock)
    {
        _internalCommandMapper = internalCommandMapper;
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _clock =
            clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task EnqueueAsync<TCommand>(TCommand command)
        where TCommand : InternalCommand
    {
        ArgumentNullException.ThrowIfNull(command);

        var data = _serializer.Serialize(command);
        var commandMetadata = _internalCommandMapper.GetByType(command.GetType());
        var queuedCommand = new QueuedInternalCommand(command.Id, commandMetadata.CommandName, data, _clock.GetCurrentInstant());
        await _context.QueuedInternalCommands.AddAsync(queuedCommand).ConfigureAwait(false);
    }
}
