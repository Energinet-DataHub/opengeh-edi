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

using System.Data;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Commands;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.IntegrationTests.Fixtures;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Messaging.IntegrationTests.Infrastructure.Configuration.InternalCommands;

[IntegrationTest]
public class InternalCommandProcessorTests : TestBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InternalCommandProcessor _processor;
    private readonly ISystemDateTimeProvider _timeProvider;
    private readonly ICommandScheduler _scheduler;
    private readonly IEdiDatabaseConnection _connection;

    public InternalCommandProcessorTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _unitOfWork = GetService<IUnitOfWork>();
        _processor = GetService<InternalCommandProcessor>();
        _timeProvider = GetService<ISystemDateTimeProvider>();
        _scheduler = GetService<ICommandScheduler>();
        _connection = GetService<IEdiDatabaseConnection>();
        var mapper = GetService<InternalCommandMapper>();
        mapper.Add(nameof(TestCommand), typeof(TestCommand));
    }

    private IDbConnection Connection => _connection.GetConnectionAndOpen();

    [Fact]
    public async Task Scheduled_commands_are_processed()
    {
        var yesterday = _timeProvider.Now().Minus(Duration.FromDays(1));
        var command = new TestCommand();
        await Schedule(command).ConfigureAwait(false);

        await ProcessPendingCommands().ConfigureAwait(false);

        AssertIsProcessed(command);
    }

    [Fact]
    public async Task When_execution_fails_the_exception_is_logged_and_command_is_marked_as_processed()
    {
        var commandThatThrows = new TestCommand(throwException: true);
        await Schedule(commandThatThrows).ConfigureAwait(false);

        await ProcessPendingCommands().ConfigureAwait(false);

        AssertIsProcessed(commandThatThrows);
        AssertHasException(commandThatThrows);
    }

    private void AssertIsProcessed(InternalCommand command)
    {
        var checkStatement =
            $"SELECT COUNT(1) FROM [b2b].[QueuedInternalCommands] WHERE Id = '{command.Id}' AND ProcessedDate IS NOT NULL";
        AssertSqlStatement(checkStatement);
    }

    private void AssertHasException(InternalCommand command)
    {
        var checkStatement =
            $"SELECT COUNT(1) FROM [b2b].[QueuedInternalCommands] WHERE Id = '{command.Id}' AND [ErrorMessage] IS NOT NULL";
        AssertSqlStatement(checkStatement);
    }

    private void AssertSqlStatement(string sqlStatement)
    {
        Assert.True(Connection.ExecuteScalar<bool>(sqlStatement));
    }

    private async Task ProcessPendingCommands()
    {
        await _processor.ProcessPendingAsync().ConfigureAwait(false);
    }

    private async Task Schedule(InternalCommand command)
    {
        await _scheduler.EnqueueAsync(command).ConfigureAwait(false);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);
    }
}
