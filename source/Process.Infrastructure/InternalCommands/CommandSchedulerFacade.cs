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

using Energinet.DataHub.EDI.Process.Domain.Commands;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;

public class CommandSchedulerFacade
{
    private readonly ICommandScheduler _commandScheduler;
    private readonly ProcessContext _processContext;

    public CommandSchedulerFacade(ICommandScheduler commandScheduler, ProcessContext processContext)
    {
        _commandScheduler = commandScheduler;
        _processContext = processContext;
    }

    public async Task EnqueueAsync(InternalCommand command)
    {
        await _commandScheduler.EnqueueAsync(command).ConfigureAwait(false);
        await _processContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
