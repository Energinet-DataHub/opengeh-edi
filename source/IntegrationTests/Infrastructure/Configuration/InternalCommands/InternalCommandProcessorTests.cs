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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration;
using Application.Configuration.Commands;
using Application.Configuration.Commands.Commands;
using Application.Configuration.DataAccess;
using Dapper;
using Infrastructure.Configuration.InternalCommands;
using IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Infrastructure.Configuration.InternalCommands;

[IntegrationTest]
public class InternalCommandProcessorTests : TestBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InternalCommandProcessor _processor;
    private readonly ISystemDateTimeProvider _timeProvider;
    private readonly ICommandScheduler _scheduler;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public InternalCommandProcessorTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _unitOfWork = GetService<IUnitOfWork>();
        _processor = GetService<InternalCommandProcessor>();
        _timeProvider = GetService<ISystemDateTimeProvider>();
        _scheduler = GetService<ICommandScheduler>();
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
        var mapper = GetService<InternalCommandMapper>();
        mapper.Add(nameof(TestCommand), typeof(TestCommand));
        mapper.Add(nameof(TestCreateOutgoingMessageCommand), typeof(TestCreateOutgoingMessageCommand));
    }

    private IDbConnection Connection => _connectionFactory.GetConnectionAndOpen();

    [Fact]
    public async Task Scheduled_commands_are_processed()
    {
        var yesterday = _timeProvider.Now().Minus(Duration.FromDays(1));
        var command = new TestCommand();
        await Schedule(command).ConfigureAwait(false);

        await ProcessPendingCommands().ConfigureAwait(false);

        AssertIsProcessedSuccessful(command);
    }

    [Fact]
    public async Task When_execution_fails_the_exception_is_logged_and_command_is_marked_as_processed()
    {
        var commandThatThrows = new TestCommand(throwException: true);
        await Schedule(commandThatThrows).ConfigureAwait(false);

        await ProcessPendingCommands().ConfigureAwait(false);

        AssertHasErrorMessage(commandThatThrows);
    }

    [Fact]
    public async Task Processing_same_command_twice_should_result_in_one_outgoing_message()
    {
        // Arrange
        var serviceScopeFactory = GetService<IServiceScopeFactory>();
        using var newScope = serviceScopeFactory.CreateScope();
        var internalCommandProcessor = newScope.ServiceProvider.GetRequiredService<InternalCommandProcessor>();
        var command = new TestCreateOutgoingMessageCommand(1);

        await Schedule(command).ConfigureAwait(false);

        var task1 = ProcessPendingCommands();

         // NEW SCOPE for second task.
        var task2 = internalCommandProcessor.ProcessPendingAsync(CancellationToken.None);

        await Task.WhenAll(task1, task2).ConfigureAwait(false);
        AssertSingleActorMessageQueue();
        AssertOutgoingMessage(1);
        AssertIsProcessedSuccessful(command);
    }

    [Fact]
    public async Task Ensure_a_single_actor_queue_is_created_for_multiple_outgoing_message()
    {
        // Arrange
        var command = new TestCreateOutgoingMessageCommand(2);

        await Schedule(command).ConfigureAwait(false);

        await ProcessPendingCommands().ConfigureAwait(false);

        AssertSingleActorMessageQueue();
        AssertOutgoingMessage(2);
        AssertIsProcessedSuccessful(command);
    }

    private void AssertSingleActorMessageQueue()
    {
        var checkStatement =
            $"SELECT COUNT(*) FROM [dbo].[ActorMessageQueues]";

        Assert.Equal(1, Connection.ExecuteScalar<int>(checkStatement));
    }

    private void AssertOutgoingMessage(int exceptedCount)
    {
        var checkStatement =
            $"SELECT COUNT(*) FROM [dbo].[OutgoingMessages]";

        Assert.Equal(exceptedCount, Connection.ExecuteScalar<int>(checkStatement));
    }

    private void AssertIsProcessedSuccessful(InternalCommand command)
    {
        var checkStatement =
            $"SELECT COUNT(1) FROM [dbo].[QueuedInternalCommands] WHERE Id = '{command.Id}' AND ProcessedDate IS NOT NULL AND [ErrorMessage] IS NULL";
        AssertSqlStatement(checkStatement);
    }

    private void AssertHasErrorMessage(InternalCommand command)
    {
        var checkStatement =
            $"SELECT COUNT(1) FROM [dbo].[QueuedInternalCommands] WHERE Id = '{command.Id}' AND [ErrorMessage] IS NOT NULL AND ProcessedDate IS NOT NULL";
        AssertSqlStatement(checkStatement);
    }

    private void AssertSqlStatement(string sqlStatement)
    {
        Assert.True(Connection.ExecuteScalar<bool>(sqlStatement));
    }

    private async Task ProcessPendingCommands()
    {
        await _processor.ProcessPendingAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task Schedule(InternalCommand command)
    {
        await _scheduler.EnqueueAsync(command).ConfigureAwait(false);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);
    }
}
