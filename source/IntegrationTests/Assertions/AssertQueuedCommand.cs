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
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using Xunit;
using Xunit.Sdk;

namespace Energinet.DataHub.EDI.IntegrationTests.Assertions;

public class AssertQueuedCommand
{
    private readonly ISerializer _serializer = new Serializer();
    private readonly IReadOnlyList<string> _commandPayload;
    private readonly Type _commandType;

    private AssertQueuedCommand(IReadOnlyList<string> commandPayload, Type commandType)
    {
        _commandPayload = commandPayload;
        _commandType = commandType;
    }

    public static AssertQueuedCommand QueuedCommand<TCommandType>(IDatabaseConnectionFactory connectionFactory, InternalCommandMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

        var commandMetadata = mapper.GetByType(typeof(TCommandType));
        var sql =
            $"SELECT Data FROM [dbo].[QueuedInternalCommands] WHERE Type = @CommandType";
        using var dbConnection = connectionFactory.GetConnectionAndOpen();
        var commandPayloads = dbConnection
            .Query<string>(
            sql,
            new { CommandType = commandMetadata.CommandName, })
            .ToList();

        Assert.NotEmpty(commandPayloads);
        return new AssertQueuedCommand(commandPayloads, typeof(TCommandType));
    }

    public AssertQueuedCommand HasProperty<TCommandType>(Func<TCommandType, object> propertySelector, object expectedValue)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);
        if (_commandPayload.Count > 1)
        {
            throw new XunitException(
                "Found more than 1 internal command. This assertion can be on single commands only");
        }

        var sut = _serializer.Deserialize<TCommandType>(_commandPayload[0]);
        Assert.Equal(expectedValue, propertySelector(sut));
        return this;
    }

    public AssertQueuedCommand CountIs(int expectedNumberOfCommands)
    {
        Assert.Equal(expectedNumberOfCommands, _commandPayload.Count);
        return this;
    }
}
