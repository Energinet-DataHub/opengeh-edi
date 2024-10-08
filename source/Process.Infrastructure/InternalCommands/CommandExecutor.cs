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

using Energinet.DataHub.EDI.Process.Domain.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;

public class CommandExecutor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CommandExecutor(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task ExecuteAsync(InternalCommand command, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command, cancellationToken).ConfigureAwait(false);
    }
}
