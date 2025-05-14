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
    private readonly EnqueueActorMessagesHttpDsl _enqueueActorMessagesForEnergySupplier;
    private readonly EnqueueActorMessagesHttpDsl _enqueueActorMessagesForGridAccessProvider;

    private readonly Actor _energySupplier = new Actor(
        actorNumber: ActorNumber.Create(SubsystemTestFixture.EdiSubsystemTestCimEnergySupplierNumber),
        actorRole: ActorRole.EnergySupplier);

    private readonly Actor _gridAccessProvider = new Actor(
        actorNumber: ActorNumber.Create(SubsystemTestFixture.EdiSubsystemTestCimGridAccessProviderNumber),
        actorRole: ActorRole.GridAccessProvider);

    public WhenEnqueueActorMessagesViaHttpTests(SubsystemTestFixture fixture, ITestOutputHelper output)
        : base(output, fixture)
    {
        var subsystemHttpDriver = new SubsystemDriver(
            fixture.SubsystemHttpClient,
            output);

        var ediDriverForEnergySupplier = new EdiDriver(
            fixture.DurableClient,
            fixture.B2BClients.EnergySupplier,
            output);

        _enqueueActorMessagesForEnergySupplier = new EnqueueActorMessagesHttpDsl(
            ediDriverForEnergySupplier,
            subsystemHttpDriver);

        var ediDriverForGridAccessProvider = new EdiDriver(
            fixture.DurableClient,
            fixture.B2BClients.GridAccessProvider,
            output);

        _enqueueActorMessagesForGridAccessProvider = new EnqueueActorMessagesHttpDsl(
            ediDriverForGridAccessProvider,
            subsystemHttpDriver);
    }

    [Fact]
    public async Task Actor_can_peek_electrical_heating_message()
    {
        const string meteringPointId = "1234567890123456";

        await _enqueueActorMessagesForEnergySupplier.EnqueueElectricalHeatingMessage(_energySupplier, meteringPointId);

        await _enqueueActorMessagesForEnergySupplier.ConfirmRsm012MessageIsAvailable(meteringPointId);
    }

    [Fact]
    public async Task Actor_can_peek_missing_measurements_message()
    {
        await _enqueueActorMessagesForGridAccessProvider.EnqueueMissingMeasurementsMessage(_gridAccessProvider);

        // TODO #751: Re-introduce when enqueue & JSON document writer is complete
        // await _enqueueActorMessagesForGridAccessProvider.ConfirmRsm018MessageIsAvailable();
    }
}
