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
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Xunit;

namespace Messaging.IntegrationTests.Assertions;

public class AssertQueuedCommand
{
    private readonly IDbConnectionFactory _connectionFactory;

    private AssertQueuedCommand(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public static AssertQueuedCommand QueuedCommand<TCommandType>(IDbConnectionFactory connectionFactory)
    {
        if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
        var sql =
            $"SELECT COUNT(1) FROM [b2b].[QueuedInternalCommands] WHERE Type = @CommandType";
        var found = connectionFactory.GetOpenConnection().ExecuteScalar<bool>(
            sql,
            new { CommandType = typeof(TCommandType).AssemblyQualifiedName, });

        Assert.True(found);
        return new AssertQueuedCommand(connectionFactory);
    }
}
