﻿// Copyright 2020 Energinet DataHub A/S
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
public sealed class WhenMonthlyAmountPerChargeResultResultIsPublishedTests
{
    private readonly AggregationResultDsl _aggregations;

    public WhenMonthlyAmountPerChargeResultResultIsPublishedTests(AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _aggregations = new AggregationResultDsl(
            new EdiDriver(fixture.B2BEnergySupplierAuthorizedHttpClient),
            new WholesaleDriver(fixture.EventPublisher));
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_monthly_charge_result()
    {
        await _aggregations.EmptyQueueForActor();

        await _aggregations.PublishMonthlyChargeResultFor(
            AcceptanceTestFixture.CimActorGridArea,
            AcceptanceTestFixture.EdiSubsystemTestCimActorNumber,
            AcceptanceTestFixture.ActorNumber);

        await _aggregations.ConfirmResultIsAvailableFor();
    }
}
