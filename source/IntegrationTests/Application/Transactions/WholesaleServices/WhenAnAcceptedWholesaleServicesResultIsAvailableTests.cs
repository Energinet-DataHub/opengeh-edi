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

using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.WholesaleServices;

[IntegrationTest]
public sealed class WhenAnAcceptedWholesaleServicesResultIsAvailableTests : TestBase
{
    private readonly ProcessContext _processContext;

    public WhenAnAcceptedWholesaleServicesResultIsAvailableTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processContext = GetService<ProcessContext>();
    }

    [Fact]
    public async Task Received_same_accepted_wholesale_services_event_twice_enqueues_1_message()
    {
        // Arrange
        var process = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .Build();
        await Store(process);
        var acceptedEvent = WholesaleServicesRequestAcceptedBuilder(process)
            .Build();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessages = await OutgoingMessagesAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessages.Count.Should().Be(1);
    }

    [Fact]
    public async Task Received_accepted_wholesale_services_event_when_process_is_rejected_enqueues_0_message()
    {
        // Arrange
        var process = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Rejected)
            .Build();
        await Store(process);
        var acceptedEvent = WholesaleServicesRequestAcceptedBuilder(process)
            .Build();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessages = await OutgoingMessagesAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessages.Count.Should().Be(0);
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

    private async Task<IReadOnlyCollection<dynamic>> OutgoingMessagesAsync(
        ActorRole receiverRole,
        BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);
        ArgumentNullException.ThrowIfNull(receiverRole);

        var connectionFactoryFactory = GetService<IDatabaseConnectionFactory>();
        using var connection = await connectionFactoryFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        var messages = await connection.QueryAsync(
            $"SELECT m.Id, m.RecordId, m.DocumentType, m.DocumentReceiverNumber, m.DocumentReceiverRole, m.ReceiverNumber, m.ProcessId, m.BusinessReason," +
            $"m.ReceiverRole, m.SenderId, m.SenderRole, m.FileStorageReference, m.RelatedToMessageId " +
            $" FROM [dbo].[OutgoingMessages] m" +
            $" WHERE m.DocumentType = '{DocumentType.NotifyWholesaleServices.Name}' AND m.BusinessReason = '{businessReason.Name}' AND m.ReceiverRole = '{receiverRole.Code}'");

        return messages.ToList().AsReadOnly();
    }

    private async Task Store(WholesaleServicesProcess process)
    {
        _processContext.WholesaleServicesProcesses.Add(process);
        await _processContext.SaveChangesAsync();
    }
}
