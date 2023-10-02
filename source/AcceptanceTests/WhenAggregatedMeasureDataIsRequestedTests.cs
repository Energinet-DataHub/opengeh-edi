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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
public sealed class WhenAggregatedMeasureDataIsRequestedTests : TestRunner
{
    private readonly AggregatedMeasureDataRequestDsl _aggregationRequest;

    public WhenAggregatedMeasureDataIsRequestedTests()
    {
        _aggregationRequest = new AggregatedMeasureDataRequestDsl(new EdiDriver(AzpToken));
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_message_after_aggregated_measure_data_has_been_requested()
    {
        await _aggregationRequest.EmptyQueueForActor(actorNumber: "5790000610976", actorRole: "metereddataresponsible");

        await _aggregationRequest.AggregatedMeasureDataFor(actorNumber: "5790000610976", actorRole: "metereddataresponsible");

        await _aggregationRequest.ConfirmAcceptedResultIsAvailableFor(actorNumber: "5790000610976", actorRole: "metereddataresponsible");
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_rejected_message_after_aggregated_measure_data_has_been_requested()
    {
        await _aggregationRequest.EmptyQueueForActor(actorNumber: "5790000610976", actorRole: "metereddataresponsible");

        await _aggregationRequest.RejectedAggregatedMeasureDataFor(actorNumber: "5790000610976", actorRole: "metereddataresponsible");

        await _aggregationRequest.ConfirmRejectedResultIsAvailableFor(actorNumber: "5790000610976", actorRole: "metereddataresponsible");
    }
}
