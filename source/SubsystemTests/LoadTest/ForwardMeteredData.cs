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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.LoadTest;

/// <summary>
/// Test class used in the CI to trigger a calculation completed event, used for load testing on t001.
/// GitHub action should be as following:
/// 1. Run Before_load_test() test
/// 2. Start Azure Load Test
/// 3. Wait for the Azure Load Test to finish
/// 4. Run After_load_test() test
/// </summary>
[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test class")]
public sealed class ForwardMeteredData : IClassFixture<LoadTestFixture>
{
    private const string ForwardMeteredEnqueuedAmountMetric = "ForwardMeteredEnqueuedAmount";
    private readonly LoadTestFixture _fixture;
    private readonly ITestOutputHelper _logger;
    private readonly EdiDatabaseDriver _ediDatabaseDriver;

    public ForwardMeteredData(LoadTestFixture fixture, ITestOutputHelper logger)
    {
        _fixture = fixture;
        _logger = logger;
        _ediDatabaseDriver = new EdiDatabaseDriver(_fixture.DatabaseConnectionString);
    }

    [Fact]
    public Task Before_load_test()
    {
        // Nothing to do before the load test
        return Task.CompletedTask;
    }

    [Fact]
    public async Task After_load_test()
    {
        var enqueuedMessagesCount = await _ediDatabaseDriver.CountEnqueuedNotifyValidatedMeasureDataMessagesFromLoadTestAsync();
        _logger.WriteLine($"Enqueued messages count: {enqueuedMessagesCount}");

        _fixture.TelemetryClient.GetMetric(ForwardMeteredEnqueuedAmountMetric).TrackValue(enqueuedMessagesCount);

        using var scope = new AssertionScope();
        enqueuedMessagesCount.Should().BeGreaterThanOrEqualTo(
            _fixture.MinimumEnqueuedMessagesCount,
            $"because the system should be performant enough to enqueue at least {_fixture.MinimumEnqueuedMessagesCount} messages during the load test");

        await CleanUp();
    }

    private async Task CleanUp()
    {
        await _ediDatabaseDriver.MarkBundlesFromLoadTestAsDequeuedAMonthAgoAsync(CancellationToken.None);
    }
}
