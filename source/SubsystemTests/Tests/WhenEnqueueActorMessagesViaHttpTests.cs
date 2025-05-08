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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Dsl;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests;

[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
public class WhenEnqueueActorMessagesViaHttpTests : BaseTestClass
{
    private readonly EnqueueActorMessagesHttpDsl _enqueueActorMessagesHttpDsl;
    private readonly Actor _receiver = new Actor(
        actorNumber: ActorNumber.Create(SubsystemTestFixture.EdiSubsystemTestCimEnergySupplierNumber),
        actorRole: ActorRole.EnergySupplier);

    public WhenEnqueueActorMessagesViaHttpTests(SubsystemTestFixture fixture, ITestOutputHelper output)
        : base(output, fixture)
    {
        var ediDriver = new EdiDriver(
            fixture.DurableClient,
            fixture.B2BClients.EnergySupplier,
            output,
            fixture.SubsystemHttpClient);

        _enqueueActorMessagesHttpDsl = new EnqueueActorMessagesHttpDsl(ediDriver);
    }

    [Fact]
    public async Task Actor_can_peek_messages_enqueue_via_http()
    {
        var meteringPointId = "1234567890123456";

        await _enqueueActorMessagesHttpDsl
            .EnqueueCalculatedMeasurementMessage(
                _receiver,
                meteringPointId);

        await _enqueueActorMessagesHttpDsl.ConfirmResponseIsAvailable(meteringPointId);
    }
}
