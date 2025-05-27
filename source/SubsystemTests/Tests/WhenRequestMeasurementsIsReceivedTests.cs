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
using Energinet.DataHub.EDI.SubsystemTests.Drivers.Ebix;
using Energinet.DataHub.EDI.SubsystemTests.Dsl;
using Energinet.DataHub.EDI.SubsystemTests.TestOrdering;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests;

[TestCaseOrderer(
    ordererTypeName: "Energinet.DataHub.EDI.SubsystemTests.TestOrdering.TestOrderer",
    ordererAssemblyName: "Energinet.DataHub.EDI.SubsystemTests")]
[IntegrationTest]
[IntegrationTest]
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
public class WhenRequestMeasurementsIsReceivedTests : BaseTestClass
{
    private readonly RequestMeasurementsDsl _forwardMeteredDataAsEnergySupplier;

    public WhenRequestMeasurementsIsReceivedTests(ITestOutputHelper output, SubsystemTestFixture fixture)
        : base(output, fixture)
    {
        _forwardMeteredDataAsEnergySupplier = new RequestMeasurementsDsl(
            new EbixDriver(
                fixture.EbixUri,
                fixture.EbixEnergySupplierCredentials,
                output),
            new EdiDriver(
                fixture.DurableClient,
                fixture.B2BClients.EnergySupplier,
                output),
            new EdiDatabaseDriver(fixture.ConnectionString),
            new ProcessManagerDriver(fixture.EdiTopicClient));
    }

    [Fact]
    [Order(100)] // Default is 0, hence we assign this a higher number => it will run last, and therefor not interfere with the other tests
    public async Task Actor_sends_request_measurements_in_cim_json_to_datahub()
    {
        var messageId = await _forwardMeteredDataAsEnergySupplier
            .SendRequestMeasurementsInCimAsync();

        await _forwardMeteredDataAsEnergySupplier.ConfirmRequestIsReceivedAsync(
            messageId,
            CancellationToken.None);
    }

    [Fact]
    public async Task Actor_can_peek_and_dequeue_forward_metered_data_response()
    {
        await _forwardMeteredDataAsEnergySupplier.PublishEnqueueBrs024AcceptedMeasurements(
            new Actor(
                actorNumber: ActorNumber.Create(SubsystemTestFixture.EdiSubsystemTestCimEnergySupplierNumber),
                actorRole: ActorRole.EnergySupplier));

        await _forwardMeteredDataAsEnergySupplier.ConfirmResponseIsAvailable();
    }
}
