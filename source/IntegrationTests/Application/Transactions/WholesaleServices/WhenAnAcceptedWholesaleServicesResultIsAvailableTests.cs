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

using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.WholesaleServices;

[IntegrationTest]
public class WhenAnAcceptedWholesaleServicesResultIsAvailableTests : TestBase
{
    private readonly ProcessContext _processContext;

    public WhenAnAcceptedWholesaleServicesResultIsAvailableTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _processContext = GetService<ProcessContext>();
    }

    [Fact]
    public async Task Received_accepted_wholesale_services_event_enqueues_message()
    {
        // Arrange
        var process = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .Build();
        Store(process);
        var acceptedEvent = WholesaleServicesRequestAcceptedBuilder(process)
            .Build();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessage.Should().NotBeNull();
    }

    [Fact]
    public void Received_same_accepted_wholesale_services_event_twice_enqueues_1_message()
    {
        1.Should().Be(2);
    }

    [Fact]
    public void Received_accepted_wholesale_services_event_when_process_is_rejected_enqueues_0_message()
    {
        1.Should().Be(2);
    }

    [Fact]
    public void Received_2_accepted_wholesale_services_events__enqueues_1_message()
    {
        1.Should().Be(2);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
    }

    private static WholesaleServicesRequestAcceptedBuilder WholesaleServicesRequestAcceptedBuilder(WholesaleServicesProcess process)
    {
        return new WholesaleServicesRequestAcceptedBuilder(process);
    }

    private static WholesaleServicesProcessBuilder WholesaleServicesProcessBuilder()
    {
        return new WholesaleServicesProcessBuilder();
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(
        ActorRole roleOfReceiver,
        BusinessReason businessReason)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyWholesaleServices.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>(),
            GetService<IFileStorageClient>());
    }

    private void Store(WholesaleServicesProcess process)
    {
        _processContext.WholesaleServicesProcesses.Add(process);
        _processContext.SaveChanges();
    }
}
