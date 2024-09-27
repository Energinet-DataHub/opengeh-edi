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

using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Dsl;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests;

/// <summary>
/// Test class used in the CI to trigger a calculation completed event, used for performance testing on t001.
/// </summary>
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
public sealed class PerformanceTestHelperTests
{
    private readonly CalculationCompletedDsl _calculationCompleted;

    public PerformanceTestHelperTests(SubsystemTestFixture fixture, ITestOutputHelper output)
    {
        _calculationCompleted = new CalculationCompletedDsl(
            new EdiDriver(
                fixture.DurableClient,
                fixture.B2BClients.MeteredDataResponsible,
                output),
            new EdiDatabaseDriver(fixture.ConnectionString),
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient),
            output,
            fixture.BalanceFixingCalculationId,
            fixture.WholesaleFixingCalculationId);
    }

    [Fact]
    public async Task Send_calculation_completed_event()
    {
        var performanceTestCalculationId = Guid.Parse("c0dc2726-168f-4eb0-a072-29ff97bb32f1");
        await _calculationCompleted.PublishForCalculationId(
            performanceTestCalculationId,
            CalculationCompletedV1.Types.CalculationType.WholesaleFixing);
    }
}
