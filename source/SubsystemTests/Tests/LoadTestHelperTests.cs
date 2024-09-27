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
public sealed class LoadTestHelperTests
{
    private readonly Guid _loadTestCalculationId = Guid.Parse("c0dc2726-168f-4eb0-a072-29ff97bb32f1");

    private readonly SubsystemTestFixture _fixture;
    private readonly CalculationCompletedDsl _calculationCompleted;

    public LoadTestHelperTests(SubsystemTestFixture fixture, ITestOutputHelper output)
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
    public async Task Prepare_load_test()
    {
        var orchestrationId = await _calculationCompleted.PublishForCalculationId(
            _loadTestCalculationId,
            CalculationCompletedV1.Types.CalculationType.WholesaleFixing);

        _fixture.LoadTestOrchestrationId = orchestrationId;
    }

    [Fact]
    public async Task Cleanup_load_test()
    {
        if (_fixture.LoadTestOrchestrationId == null)
            throw new Exception("Load test orchestration id is not set");

        // TODO: Stop orchestration
        // TODO: Remove outgoing messages for calculation (_loadTestCalculationId)

        await Task.CompletedTask;
    }
}
