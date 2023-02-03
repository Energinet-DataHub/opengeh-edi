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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Application.Configuration.DataAccess;
using Dapper;
using Infrastructure.Configuration.InternalCommands;
using Xunit;

namespace IntegrationTests.Assertions;

public class AssertQueuedCommand
{
    private readonly IReadOnlyList<string> _commandPayload;

    private AssertQueuedCommand(IReadOnlyList<string> commandPayload)
    {
        _commandPayload = commandPayload;
    }

    public static AssertQueuedCommand QueuedCommand<TCommandType>(IDatabaseConnectionFactory connectionFactory, InternalCommandMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

        var commandMetadata = mapper.GetByType(typeof(TCommandType));
        var sql =
            $"SELECT Data FROM [dbo].[QueuedInternalCommands] WHERE Type = @CommandType";
        var commandPayloads = connectionFactory.GetConnectionAndOpen()
            .Query<string>(
            sql,
            new { CommandType = commandMetadata.CommandName, })
            .ToList();

        Assert.NotEmpty(commandPayloads);
        return new AssertQueuedCommand(commandPayloads);
    }

    public AssertQueuedCommand CountIs(int expectedNumberOfCommands)
    {
        Assert.Equal(expectedNumberOfCommands, _commandPayload.Count);
        return this;
    }
}
