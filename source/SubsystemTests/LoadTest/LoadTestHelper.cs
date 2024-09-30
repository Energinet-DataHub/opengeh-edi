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
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test class")]
public sealed class LoadTestHelper
{
    private readonly SubsystemTestFixture _fixture;
    private readonly CalculationCompletedDsl _calculationCompleted;

    public LoadTestHelper(SubsystemTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;

        _calculationCompleted = new CalculationCompletedDsl(
            new EdiDriver(
                _fixture.DurableClient,
                _fixture.B2BClients.MeteredDataResponsible,
                output),
            new EdiDatabaseDriver(_fixture.ConnectionString),
            new WholesaleDriver(_fixture.EventPublisher, _fixture.EdiInboxClient),
            output,
            _fixture.BalanceFixingCalculationId,
            _fixture.WholesaleFixingCalculationId);
    }

    [Fact]
    public async Task Pre_load_test()
    {
        await _calculationCompleted.PublishForCalculation(
            _fixture.LoadTestCalculationId,
            CalculationType.WholesaleFixing);
    }
}
