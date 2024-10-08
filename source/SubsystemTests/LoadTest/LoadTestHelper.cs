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
using Energinet.DataHub.EDI.SubsystemTests.Dsl;
using FluentAssertions;
using Nito.AsyncEx;
using NodaTime;
using Xunit.Abstractions;

using CalculationType = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.CalculationCompletedV1.Types.CalculationType;

namespace Energinet.DataHub.EDI.SubsystemTests.LoadTest;

/// <summary>
/// Test class used in the CI to trigger a calculation completed event, used for load testing on t001.
/// GitHub action should be as following:
/// 1. Run Prepare_load_test() test
/// 2. Start Azure Load Test
/// 3. Wait for the Azure Load Test to finish
/// 4. Run Cleanup_load_test() test
/// </summary>
[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test class")]
public sealed class LoadTestHelper : IClassFixture<LoadTestFixture>
{
    private readonly LoadTestFixture _fixture;
    private readonly ITestOutputHelper _logger;
    private readonly EdiDriver _ediDriver;
    private readonly EdiDatabaseDriver _ediDatabaseDriver;
    private readonly WholesaleDriver _wholesaleDriver;

    public LoadTestHelper(LoadTestFixture fixture, ITestOutputHelper logger)
    {
        _fixture = fixture;
        _logger = logger;

        _ediDriver = new EdiDriver(
            _fixture.DurableClient,
            new AsyncLazy<HttpClient>(() => throw new NotImplementedException("Not used in load test")),
            logger);
        _ediDatabaseDriver = new EdiDatabaseDriver(_fixture.DatabaseConnectionString);
        _wholesaleDriver = new WholesaleDriver(_fixture.IntegrationEventPublisher, _fixture.EdiInboxClient);
    }

    [Fact]
    public async Task Before_load_test()
    {
        await _ediDatabaseDriver.DeleteOutgoingMessagesForCalculationAsync(_fixture.LoadTestCalculationId);
        await CalculationCompletedDsl.StartEnqueueMessagesOrchestration(
            _logger,
            _wholesaleDriver,
            _ediDriver,
            CalculationType.WholesaleFixing,
            _fixture.LoadTestCalculationId);
    }

    [Fact]
    public async Task After_load_test()
    {
        await _ediDriver.StopOrchestrationForCalculationAsync(
            calculationId: _fixture.LoadTestCalculationId,
            createdAfter: SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromHours(1)));

        var dequeuedMessagesCount = await _ediDatabaseDriver.CountDequeuedMessagesForCalculationAsync(_fixture.LoadTestCalculationId);
        _logger.WriteLine($"Dequeued messages count: {dequeuedMessagesCount} (CalculationId={_fixture.LoadTestCalculationId})");

        dequeuedMessagesCount.Should().BeGreaterThanOrEqualTo(
            _fixture.MinimumDequeuedMessagesCount,
            $"because the system should be performant enough to dequeue at least {_fixture.MinimumDequeuedMessagesCount} messages during the load test");
    }
}
