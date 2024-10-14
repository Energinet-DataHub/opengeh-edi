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

using Energinet.DataHub.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.B2BApi.DataRetention;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using IServiceScopeFactory = Microsoft.Extensions.DependencyInjection.IServiceScopeFactory;

namespace Energinet.DataHub.EDI.IntegrationTests.DataRetention;

public class ExecuteDataRetentionsTests : TestBase
{
    public ExecuteDataRetentionsTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task When_DataRetentionIsExecuted_Then_NoExceptionsAreThrown()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var notification = new ADayHasPassed(GetService<IClock>().GetCurrentInstant());

        var sut = new ExecuteDataRetentionsWhenADayHasPassed(
            GetServices<IDataRetention>(),
            GetService<ILogger<ExecuteDataRetentionsWhenADayHasPassed>>(),
            GetService<IServiceScopeFactory>());

        // Act
        var act = () => sut.Handle(notification, cancellationToken);

        // Assert
        await act.Should().NotThrowAsync();

        var loggerSpy = GetService<LoggerSpy>();
        loggerSpy.CapturedExceptions.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_ADayHasPassed_When_AJobExecutionTimeLimitExceeds_Then_JobsIsCancelledSuccessfully()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var notification = new ADayHasPassed(GetService<IClock>().GetCurrentInstant());

        var sut = new ExecuteDataRetentionsWhenADayHasPassed(
            GetServices<IDataRetention>(),
            GetService<ILogger<ExecuteDataRetentionsWhenADayHasPassed>>(),
            GetService<IServiceScopeFactory>(),
            executionTimeLimitInSeconds: 1);

        // Act
        var act = () => sut.Handle(notification, cancellationToken);

        // Assert
        await act.Should().NotThrowAsync();

        var loggerSpy = GetService<LoggerSpy>();
        loggerSpy.CapturedExceptions.Should()
            .SatisfyRespectively(
                log =>
                {
                    log.Exception.Should().BeOfType<TaskCanceledException>();
                    log.LogLevel.Should().Be(LogLevel.Error);
                    log.Message
                        .Should()
                        .Be($"Data retention job {typeof(SleepyDataRetentionJob).FullName} was cancelled.");
                });
    }
}
