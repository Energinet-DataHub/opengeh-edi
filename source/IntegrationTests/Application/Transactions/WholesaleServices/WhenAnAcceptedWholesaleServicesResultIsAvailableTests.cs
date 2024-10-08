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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;

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
        var eventId = Guid.NewGuid().ToString();
        var process = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .Build();
        await Store(process);
        var acceptedEvent = WholesaleServicesRequestAcceptedBuilder(process)
            .Build();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id, eventId);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessage.Should().NotBeNull();
        outgoingMessage
            .HasProcessId(process.ProcessId)
            .HasEventId(eventId)
            .HasReceiverId(process.RequestedByActor.ActorNumber.Value)
            .HasDocumentReceiverId(process.OriginalActor.ActorNumber.Value)
            .HasReceiverRole(process.RequestedByActor.ActorRole.Code)
            .HasDocumentReceiverRole(process.OriginalActor.ActorRole.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasRelationTo(process.InitiatedByMessageId)
            .HasGridAreaCode(acceptedEvent.Series.Single().GridArea)
            .HasBusinessReason(process.BusinessReason)
            .HasProcessType(ProcessType.RequestWholesaleResults)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.Period.Start.ToString(), process.StartOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.Period.End.ToString(), process.EndOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.GridAreaCode, process.RequestedGridArea)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.MeteringPointType, MeteringPointType.Production)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.ChargeOwner!.Value, process.ChargeOwner)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(timeSeries => timeSeries.EnergySupplier.Value, process.EnergySupplierId)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.OriginalTransactionIdReference,
                process.BusinessTransactionId);
    }

    [Fact]
    public async Task Received_accepted_wholesale_monthly_sum_event_enqueues_message()
    {
        // Arrange
        var process = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .Build();

        await Store(process);

        var acceptedEvent = WholesaleServicesRequestAcceptedBuilder(process)
            .BuildMonthlySum();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessage.Should().NotBeNull();
        outgoingMessage
            .HasReceiverId(process.RequestedByActor.ActorNumber.Value)
            .HasDocumentReceiverId(process.OriginalActor.ActorNumber.Value)
            .HasReceiverRole(process.RequestedByActor.ActorRole.Code)
            .HasDocumentReceiverRole(process.OriginalActor.ActorRole.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasRelationTo(process.InitiatedByMessageId)
            .HasGridAreaCode(acceptedEvent.Series.Single().GridArea)
            .HasBusinessReason(process.BusinessReason)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.Period.Start.ToString(),
                process.StartOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.Period.End.ToString(),
                process.EndOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.GridAreaCode,
                process.RequestedGridArea)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.ChargeOwner!.Value,
                process.ChargeOwner)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.EnergySupplier.Value,
                process.EnergySupplierId)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.OriginalTransactionIdReference,
                process.BusinessTransactionId)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.ChargeCode,
                "EA-003")
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.ChargeType,
                ChargeType.Tariff)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.QuantityMeasureUnit,
                MeasurementUnit.Kwh)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.MeteringPointType,
                null)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.SettlementMethod,
                null);
    }

    [Fact]
    public async Task Received_accepted_wholesale_total_event_enqueues_message_without_charge_owner()
    {
        // Arrange
        var process = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .Build();

        await Store(process);

        var acceptedEvent = WholesaleServicesRequestAcceptedBuilder(process)
            .BuildTotalSum();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessage.Should().NotBeNull();
        outgoingMessage
            .HasReceiverId(process.RequestedByActor.ActorNumber.Value)
            .HasDocumentReceiverId(process.OriginalActor.ActorNumber.Value)
            .HasReceiverRole(process.RequestedByActor.ActorRole.Code)
            .HasDocumentReceiverRole(process.OriginalActor.ActorRole.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasRelationTo(process.InitiatedByMessageId)
            .HasGridAreaCode(acceptedEvent.Series.Single().GridArea)
            .HasBusinessReason(process.BusinessReason)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.Period.Start.ToString(),
                process.StartOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.Period.End.ToString(),
                process.EndOfPeriod)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.GridAreaCode,
                process.RequestedGridArea)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.ChargeOwner,
                null)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.EnergySupplier.Value,
                process.EnergySupplierId)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.OriginalTransactionIdReference,
                process.BusinessTransactionId)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.ChargeCode,
                null)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.ChargeType,
                null)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.QuantityMeasureUnit,
                MeasurementUnit.Kwh) // The unit is always Kwh for total sums
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.MeteringPointType,
                null)
            .HasMessageRecordValue<AcceptedWholesaleServicesSeries>(
                timeSeries => timeSeries.SettlementMethod,
                null);
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

    [Fact]
    public async Task Received_2_accepted_wholesale_services_events_enqueues_2_message()
    {
        // Arrange
        var firstProcess = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .Build();
        await Store(firstProcess);
        var firstAcceptedEvent = WholesaleServicesRequestAcceptedBuilder(firstProcess)
            .Build();
        var secondProcess = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .SetBusinessTransactionId(TransactionId.New())
            .Build();
        await Store(secondProcess);
        var secondAcceptedEvent = WholesaleServicesRequestAcceptedBuilder(firstProcess)
            .Build();

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), firstAcceptedEvent, firstProcess.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), secondAcceptedEvent, secondProcess.ProcessId.Id);

        // Assert
        var outgoingMessages = await OutgoingMessagesAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessages.Count.Should().Be(2);
    }

    [Fact]
    public async Task Given_AcceptedInboxEventWithTwoSeries_When_ReceivingInboxEvent_Then_EachOutgoingMessageHasAUniqueTransactionId()
    {
        // Arrange
        var process = WholesaleServicesProcessBuilder()
            .SetState(WholesaleServicesProcess.State.Sent)
            .Build();
        await Store(process);
        var acceptedEvent = WholesaleServicesRequestAcceptedBuilder(process)
            .Build();
        acceptedEvent.Series.Add(acceptedEvent.Series.First());

        // Act
        await HavingReceivedInboxEventAsync(nameof(WholesaleServicesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessages = await AllOutgoingMessageAsync(ActorRole.EnergySupplier, BusinessReason.WholesaleFixing);
        outgoingMessages.Count.Should().Be(2);
        var firstMessage = outgoingMessages.First();
        var secondMessage = outgoingMessages.Last();

        var seriesIdOfFirstMessage =
            firstMessage.GetMessageValue<WholesaleServicesSeries, TransactionId>(series => series.TransactionId);
        var seriesIdOfSecondMessage =
            secondMessage.GetMessageValue<WholesaleServicesSeries, TransactionId>(series => series.TransactionId);

        seriesIdOfFirstMessage.Should().NotBe(seriesIdOfSecondMessage);
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

    private async Task<IList<AssertOutgoingMessage>> AllOutgoingMessageAsync(
        ActorRole roleOfReceiver,
        BusinessReason businessReason)
    {
        return await AssertOutgoingMessage.AllOutgoingMessagesAsync(
            DocumentType.NotifyWholesaleServices.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>(),
            GetService<IFileStorageClient>());
    }

    private async Task Store(WholesaleServicesProcess process)
    {
        _processContext.WholesaleServicesProcesses.Add(process);
        await _processContext.SaveChangesAsync();
    }
}
