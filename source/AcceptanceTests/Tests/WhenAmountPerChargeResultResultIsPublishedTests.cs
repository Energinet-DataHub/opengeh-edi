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
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

[SuppressMessage(
    "Usage",
    "CA2007",
    Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
[IntegrationTest]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public sealed class WhenAmountPerChargeResultResultIsPublishedTests
{
    private readonly NotifyWholesaleResultDsl _notifyWholesaleResultDsl;

    public WhenAmountPerChargeResultResultIsPublishedTests(AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _notifyWholesaleResultDsl = new NotifyWholesaleResultDsl(
            new EdiDriver(fixture.B2BEnergySupplierAuthorizedHttpClient),
            new WholesaleDriver(fixture.EventPublisher));
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_amount_charge_result()
    {
        await _notifyWholesaleResultDsl.EmptyQueueForActor();

        await _notifyWholesaleResultDsl.PublishAmountPerChargeResultFor(
            AcceptanceTestFixture.CimActorGridArea,
            AcceptanceTestFixture.EdiSubsystemTestCimActorNumber,
            AcceptanceTestFixture.ActorNumber);

        await _notifyWholesaleResultDsl.ConfirmResultIsAvailableFor();
    }
}
