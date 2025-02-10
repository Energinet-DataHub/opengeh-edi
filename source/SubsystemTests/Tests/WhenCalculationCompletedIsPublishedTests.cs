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
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests;

[IntegrationTest]
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
public class WhenCalculationCompletedIsPublishedTests : BaseTestClass
{
    private readonly CalculationCompletedDsl _calculationCompleted;

    public WhenCalculationCompletedIsPublishedTests(SubsystemTestFixture fixture, ITestOutputHelper output)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _calculationCompleted = new CalculationCompletedDsl(
            new EdiDriver(
                fixture.DurableClient,
                fixture.B2BClients.MeteredDataResponsible,
                output),
            new EdiDatabaseDriver(fixture.ConnectionString),
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiServiceBusClient),
            output,
            fixture.BalanceFixingCalculationId,
            fixture.WholesaleFixingCalculationId);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_energy_result_from_balance_fixing()
    {
        await _calculationCompleted.PublishForBalanceFixingCalculation();

        await _calculationCompleted.ConfirmEnergyResultsAreAvailable();
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_wholesale_and_energy_result_from_wholesale_fixing()
    {
        await _calculationCompleted.PublishForWholesaleFixingCalculation();

        await _calculationCompleted.ConfirmWholesaleResultsAndEnergyResultsAreAvailable();
    }
}
