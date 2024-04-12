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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.WholesaleServices;

[IntegrationTest]
public class WhenAnAcceptedWholesaleServicesResultIsAvailableTests : TestBase
{
    private readonly ProcessContext _processContext;

    public WhenAnAcceptedWholesaleServicesResultIsAvailableTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
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
        outgoingMessage
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasDocumentReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(process.RequestedByActorRoleCode)
            .HasDocumentReceiverRole(process.RequestedByActorRoleCode)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasRelationTo(process.InitiatedByMessageId)
            .HasGridAreaCode(process.GridAreaCode!)
            .HasBusinessReason(process.BusinessReason)
            .HasProcessType(ProcessType.RequestWholesaleResults)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.Period.Start.ToString(), process.StartOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.Period.End.ToString(), process.EndOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.GridAreaCode, process.GridAreaCode)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.MeteringPointType, MeteringPointType.Production)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.ChargeOwner.Value, process.ChargeOwner)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.EnergySupplier.Value, process.EnergySupplierId)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.OriginalTransactionIdReference, process.BusinessTransactionId.Id);
    }

    [Fact]
    public async Task Received_accepted_wholesale_monthly_sum_event_enqueues_message()
    {
        // Arrange
        var process = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .Build();
        Store(process);
        var acceptedEvent = WholesaleServicesRequestAcceptedBuilder(process)
            .BuildMonthlySum();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessage.Should().NotBeNull();
        outgoingMessage
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasDocumentReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(process.RequestedByActorRoleCode)
            .HasDocumentReceiverRole(process.RequestedByActorRoleCode)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasRelationTo(process.InitiatedByMessageId)
            .HasGridAreaCode(process.GridAreaCode!)
            .HasBusinessReason(process.BusinessReason)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.Period.Start.ToString(), process.StartOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.Period.End.ToString(), process.EndOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.GridAreaCode, process.GridAreaCode)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.ChargeOwner.Value, process.ChargeOwner)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.EnergySupplier.Value, process.EnergySupplierId)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.OriginalTransactionIdReference, process.BusinessTransactionId.Id);
    }

    [Fact]
    public async Task Received_same_accepted_wholesale_services_event_twice_enqueues_1_message()
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
        Store(process);
        var acceptedEvent = WholesaleServicesRequestAcceptedBuilder(process)
            .Build();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessages = await OutgoingMessagesAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessages.Count.Should().Be(0);
    }

    [Fact]
    public async Task Received_2_accepted_wholesale_services_events_enqueues_2_message()
    {
        // Arrange
        var firstProcess = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .Build();
        Store(firstProcess);
        var firstAcceptedEvent = WholesaleServicesRequestAcceptedBuilder(firstProcess)
            .Build();
        var secondProcess = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .SetBusinessTransactionId(Guid.NewGuid())
            .Build();
        Store(secondProcess);
        var secondAcceptedEvent = WholesaleServicesRequestAcceptedBuilder(firstProcess)
            .Build();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), firstAcceptedEvent, firstProcess.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), secondAcceptedEvent, secondProcess.ProcessId.Id);

        // Assert
        var outgoingMessages = await OutgoingMessagesAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessages.Count.Should().Be(2);
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
