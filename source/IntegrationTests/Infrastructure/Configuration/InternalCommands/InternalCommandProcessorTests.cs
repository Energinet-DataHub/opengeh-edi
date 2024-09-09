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
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Domain.Commands;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;

[IntegrationTest]
public class InternalCommandProcessorTests : TestBase
{
    private readonly InternalCommandProcessor _processor;
    private readonly ICommandScheduler _scheduler;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public InternalCommandProcessorTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processor = GetService<InternalCommandProcessor>();
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
        var command = new TestCommand();
        await Schedule(command);

        await ProcessPendingCommands();

        AssertIsProcessedSuccessful(command);
    }

    [Fact]
    public async Task When_execution_fails_the_exception_is_logged_and_command_is_marked_as_processed()
    {
        var commandThatThrows = new TestCommand(throwException: true);
        await Schedule(commandThatThrows);

        await ProcessPendingCommands();

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

        await Schedule(command);

        var task1 = ProcessPendingCommands();

         // NEW SCOPE for second task.
        var task2 = internalCommandProcessor.ProcessPendingAsync(CancellationToken.None);

        await Task.WhenAll(task1, task2);
        AssertSingleActorMessageQueue();
        AssertOutgoingMessage(1);
        AssertIsProcessedSuccessful(command);
    }

    [Fact]
    public async Task Ensure_a_single_actor_queue_is_created_for_multiple_outgoing_message()
    {
        // Arrange
        var command = new TestCreateOutgoingMessageCommand(2);

        await Schedule(command);

        await ProcessPendingCommands();

        AssertSingleActorMessageQueue();
        AssertOutgoingMessage(2);
        AssertIsProcessedSuccessful(command);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Connection.Dispose();
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
        await _processor.ProcessPendingAsync(CancellationToken.None);
    }

    private async Task Schedule(InternalCommand command)
    {
        await _scheduler.EnqueueAsync(command);
        await GetService<ProcessContext>().SaveChangesAsync();
    }
}
