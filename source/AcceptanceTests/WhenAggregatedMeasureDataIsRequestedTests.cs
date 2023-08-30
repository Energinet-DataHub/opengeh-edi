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

using AcceptanceTest.Drivers;
using AcceptanceTest.Dsl;
using Xunit.Categories;

namespace AcceptanceTest;

[IntegrationTest]
public sealed class WhenAggregatedMeasureDataIsRequestedTests : TestRunner
{
    private readonly AggregatedMeasureDataDsl _aggregatedMeasure;

    public WhenAggregatedMeasureDataIsRequestedTests()
    {
        _aggregatedMeasure = new AggregatedMeasureDataDsl(
            new EdiDriver(AzpToken, EdiInboxPublisher),
            new WholeSaleDriver(EventPublisher, EdiInboxPublisher));
    }

    [Fact]
    public async Task Actor_can_fetch_message_after_aggregated_measure_data_has_been_requested()
    {
        await _aggregatedMeasure.RequestAggregatedMeasureDataFor(actorNumber: "5790001687137", actorRole: "balanceresponsibleparty").ConfigureAwait(false);
        //await _aggregatedMeasure.RequestAggregatedMeasureDataFor(actorNumber: "5790000610976", actorRole: "metereddataresponsible").ConfigureAwait(false);

        //await _aggregatedMeasure.SendAggregatedMeasureDataToInbox().ConfigureAwait(false);
       // await _aggregatedMeasure.ConfirmResultIsAvailableFor(actorNumber: "5790001687137", actorRole: "balanceresponsibleparty").ConfigureAwait(false);
    }
}
