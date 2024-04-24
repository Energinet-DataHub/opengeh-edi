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

using Energinet.DataHub.EDI.SystemTests.Dsl;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.SystemTests.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[IntegrationTest]
[Collection(SystemTestCollection.SystemTestCollectionName)]
public sealed class WhenWholesaleServicesIsRequestedTests
{
    private readonly SystemTestFixture _fixture;
    private readonly WholesaleServicesRequestDsl _wholesaleServicesRequest;

    public WhenWholesaleServicesIsRequestedTests(SystemTestFixture fixture)
    {
        _fixture = fixture;
        ArgumentNullException.ThrowIfNull(fixture);
        _wholesaleServicesRequest = new WholesaleServicesRequestDsl(_fixture.EdiDriver);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_wholesale_services_after_wholesale_services_has_been_requested()
    {
        //RSM-017
        await _wholesaleServicesRequest.RequestWholesaleSettlementForAsync(_fixture.SystemOperator, CancellationToken.None);

        //RSM-019
        var messageId = await _wholesaleServicesRequest.ConfirmWholesaleServicesResultIsAvailableForAsync(_fixture.SystemOperator, CancellationToken.None);

        await _wholesaleServicesRequest.DequeueForAsync(_fixture.SystemOperator, messageId, CancellationToken.None);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_rejected_message_after_wholesale_services_has_been_requested()
    {
        //RSM-017
        await _wholesaleServicesRequest.InvalidRequestWholesaleSettlementForAsync(_fixture.SystemOperator, CancellationToken.None);

        //RSM-017
        var messageId = await _wholesaleServicesRequest.ConfirmRejectWholesaleServicesResultIsAvailableForAsync(_fixture.SystemOperator, CancellationToken.None);

        await _wholesaleServicesRequest.DequeueForAsync(_fixture.SystemOperator, messageId, CancellationToken.None);
    }
}
