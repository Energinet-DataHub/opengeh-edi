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

using System.Diagnostics;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Energinet.DataHub.EDI.AcceptanceTests.Tests;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
[Collection("Acceptance test collection")]
public sealed class WhenAggregationResultIsPublishedTests
{
    private readonly AggregationResultDsl _aggregations;

    public WhenAggregationResultIsPublishedTests(TestRunner runner)
    {
        Debug.Assert(runner != null, nameof(runner) + " != null");
        _aggregations = new AggregationResultDsl(
            new EdiDriver(runner.AzpToken, runner.ConnectionString),
            new WholesaleDriver(runner.EventPublisher));
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_aggregation_result()
    {
        await _aggregations.EmptyQueueForActor(actorNumber: ActorNumber, actorRole: ActorRole);

        await _aggregations.PublishResultFor(gridAreaCode: ActorGridArea);

        await _aggregations
            .ConfirmResultIsAvailableFor(actorNumber: ActorNumber, actorRole: ActorRole);
    }
}
