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
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

[SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public sealed class WhenAggregationResultIsPublishedTests : BaseTestClass
{
    private readonly NotifyAggregatedMeasureDataResultDsl _notifyAggregatedMeasureDataResult;

    public WhenAggregationResultIsPublishedTests(AcceptanceTestFixture fixture, ITestOutputHelper output)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _notifyAggregatedMeasureDataResult = new NotifyAggregatedMeasureDataResultDsl(
            new EdiDriver(
                fixture.B2BMeteredDataResponsibleAuthorizedHttpClient, output),
            new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient));
    }

    [Fact(Skip = "Dev environments uses message orchestration instead of integration events for creating actor messages")]
    public async Task Actor_can_peek_and_dequeue_aggregation_result()
    {
        await _notifyAggregatedMeasureDataResult.PublishResult(AcceptanceTestFixture.CimActorGridArea);

        await _notifyAggregatedMeasureDataResult.ConfirmResultIsAvailable();
    }
}
