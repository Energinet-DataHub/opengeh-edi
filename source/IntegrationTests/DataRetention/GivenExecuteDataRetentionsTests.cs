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

using Energinet.DataHub.EDI.B2BApi.DataRetention;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.DataRetention;

public class GivenExecuteDataRetentionsTests : TestBase
{
    public GivenExecuteDataRetentionsTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task Given_ADayHasPassed_When_JobsExecutionTimeLimitExceeds_Then_JobsIsCancelledSuccessfully()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var notification = new ADayHasPassed(GetService<IClock>().GetCurrentInstant());
        var longRunningDataRetentionJob = new SleepyDataRetentionJob(executionTimeLimitInSeconds: 5);

        var loggerSpy = new LoggerSpy();
        var sut = new ExecuteDataRetentionsWhenADayHasPassed(
            new List<IDataRetention>() { longRunningDataRetentionJob },
            loggerSpy,
            executionTimeLimitInSeconds: 1);

        // Act
        var act = () => sut.Handle(notification, cancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
        loggerSpy.CapturedException.Should().BeOfType<OperationCanceledException>();
        loggerSpy.CapturedLogLevel.Should().Be(LogLevel.Warning);
        loggerSpy.Message.Should()
            .Be($"Data retention job {longRunningDataRetentionJob.GetType().FullName} was cancelled.");
    }
}

public class SleepyDataRetentionJob(int executionTimeLimitInSeconds) : IDataRetention
{
    public Task CleanupAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < executionTimeLimitInSeconds; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        return Task.CompletedTask;
    }
}

public class LoggerSpy : ILogger<ExecuteDataRetentionsWhenADayHasPassed>
{
    public Exception? CapturedException { get; private set; }

    public LogLevel? CapturedLogLevel { get; private set; }

    public string? Message { get; private set; }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        throw new NotImplementedException();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        CapturedException = exception;
        CapturedLogLevel = logLevel;
        Message = state!.ToString();
    }
}
