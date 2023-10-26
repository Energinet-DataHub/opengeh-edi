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
using System.Reflection;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Commands.Handlers;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.WellKnownTypes;
using NodaTime.Text;
using Xunit;
using Xunit.Categories;
using Period = Energinet.DataHub.Edi.Responses.Period;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class WhenAnReceiptIsAvailableTests : TestBase
{
    private readonly B2BContext _b2BContext;

    public WhenAnReceiptIsAvailableTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
    }

    [Fact]
    public async Task Receipt_with_one_matching_response_messages()
    {
        // Arrange
        var process = BuildProcess();
        var responseMessageEvent = GetResponseMessageEvent(process);
        var receiptEvent = GetReceiptEvent(new List<AggregatedTimeSeriesRequestResponseMessage> { responseMessageEvent });

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestResponseMessage), responseMessageEvent, process.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestReceipt), receiptEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(MarketRole.BalanceResponsibleParty, BusinessReason.Correction);
        var timeSerie = responseMessageEvent;
        outgoingMessage
            .HasBusinessReason(process.BusinessReason)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(MarketRole.FromCode(process.RequestedByActorRoleCode).Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(timeSerie => timeSerie.SettlementVersion, timeSerie.SettlementVersion)
            .HasMessageRecordValue<TimeSeries>(timeSerie => timeSerie.BalanceResponsibleNumber, process.BalanceResponsibleId)
            .HasMessageRecordValue<TimeSeries>(timeSerie => timeSerie.EnergySupplierNumber, process.EnergySupplierId);

        // Assert
        var processFromDb = GetProcess(process.ProcessId.Id);
        Assert.NotNull(processFromDb);
        AssertProcessState(processFromDb, AggregatedMeasureDataProcess.State.Accepted);
        AssertNumberOfPendingMessages(processFromDb, 0);
    }

    [Fact]
    public async Task Received_2_receipt_events_for_same_aggregated_measure_data_process()
    {
        // Arrange
        var process = BuildProcess();
        var responseMessageEvent = GetResponseMessageEvent(process);
        var receiptEvent = GetReceiptEvent(new List<AggregatedTimeSeriesRequestResponseMessage> { responseMessageEvent });

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestResponseMessage), responseMessageEvent, process.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestReceipt), receiptEvent, process.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestReceipt), receiptEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(MarketRole.BalanceResponsibleParty, BusinessReason.Correction);
        var timeSerie = responseMessageEvent;
        outgoingMessage
            .HasBusinessReason(process.BusinessReason)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(MarketRole.FromCode(process.RequestedByActorRoleCode).Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(timeSerie => timeSerie.SettlementVersion!, timeSerie.SettlementVersion);
    }

    [Fact]
    public async Task Receipt_with_two_matching_response_messages()
    {
        // Arrange
        var process = BuildProcess();
        var responseMessageEvent = GetResponseMessageEvent(process);
        var responseMessageEvent1 = GetResponseMessageEvent(process);
        responseMessageEvent1.GridArea = "999";

        var receiptEvent = GetReceiptEvent(new List<AggregatedTimeSeriesRequestResponseMessage> { responseMessageEvent, responseMessageEvent1 });

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestResponseMessage), responseMessageEvent, process.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestResponseMessage), responseMessageEvent1, process.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestReceipt), receiptEvent, process.ProcessId.Id);

        // Assert
        var processFromDb = GetProcess(process.ProcessId.Id);
        Assert.NotNull(processFromDb);
        AssertOutgoingMessageCreated(processFromDb, 2);
    }

    [Fact]
    public async Task Received_receipt_events_but_too_many_response_messages()
    {
        // Arrange
        var process = BuildProcess();
        var responseMessageEvent = GetResponseMessageEvent(process);

        var receiptEvent = GetReceiptEvent(new List<AggregatedTimeSeriesRequestResponseMessage> { responseMessageEvent });

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestResponseMessage), responseMessageEvent, process.ProcessId.Id);

        var responseMessageEvent2 = responseMessageEvent;
        responseMessageEvent2.GridArea = "999";
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestResponseMessage), responseMessageEvent2, process.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestReceipt), receiptEvent, process.ProcessId.Id);

        // Assert
        var processFromDb = GetProcess(process.ProcessId.Id);
        Assert.NotNull(processFromDb);
        AssertProcessState(processFromDb, AggregatedMeasureDataProcess.State.Initialized);
        AssertNumberOfPendingMessages(processFromDb, 0);
        AssertOutgoingMessageCreated(processFromDb, 0);
    }

    [Fact]
    public async Task Received_receipt_events_but_missing_response_messages()
    {
        // Arrange
        var process = BuildProcess();
        var responseMessageEvent = GetResponseMessageEvent(process);

        var receiptEvent = GetReceiptEvent(new List<AggregatedTimeSeriesRequestResponseMessage> { responseMessageEvent });
        receiptEvent.GridAreas.Add("999");

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestResponseMessage), responseMessageEvent, process.ProcessId.Id);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestReceipt), receiptEvent, process.ProcessId.Id);

        // Assert
        var processFromDb = GetProcess(process.ProcessId.Id);
        Assert.NotNull(processFromDb);
        AssertProcessState(processFromDb, AggregatedMeasureDataProcess.State.Initialized);
        AssertNumberOfPendingMessages(processFromDb, 1);
        AssertOutgoingMessageCreated(processFromDb, 0);
        AssertSingleEnqueuedInternalCommand(nameof(SendAggregatedMeasureRequestToWholesale));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _b2BContext.Dispose();
    }

    private static AggregatedTimeSeriesRequestReceipt GetReceiptEvent(List<AggregatedTimeSeriesRequestResponseMessage> responseMessages)
    {
        var response = new AggregatedTimeSeriesRequestReceipt();
        response.GridAreas.AddRange(responseMessages.Select(response => response.GridArea));
        return response;
    }

    private static AggregatedTimeSeriesRequestResponseMessage GetResponseMessageEvent(AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
    {
        return CreateAggregation(aggregatedMeasureDataProcess);
    }

    private static AggregatedTimeSeriesRequestResponseMessage CreateAggregation(AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
    {
        var quantity = new DecimalValue() { Units = 12345, Nanos = 123450000, };
        var point = new TimeSeriesPoint()
        {
            Quantity = quantity,
            QuantityQuality = QuantityQuality.Incomplete,
            Time = new Timestamp() { Seconds = 1, },
        };

        var period = new Period()
        {
            StartOfPeriod = new Timestamp()
            {
                Seconds = InstantPattern.General.Parse(aggregatedMeasureDataProcess.StartOfPeriod)
                .GetValueOrThrow().ToUnixTimeSeconds(),
            },
            EndOfPeriod = new Timestamp()
            {
                Seconds = aggregatedMeasureDataProcess.EndOfPeriod is not null
                ? InstantPattern.General.Parse(aggregatedMeasureDataProcess.EndOfPeriod).GetValueOrThrow().ToUnixTimeSeconds()
                : 1,
            },
            Resolution = Resolution.Pt15M,
        };

        return new AggregatedTimeSeriesRequestResponseMessage()
        {
            GridArea = aggregatedMeasureDataProcess.MeteringGridAreaDomainId,
            QuantityUnit = QuantityUnit.Kwh,
            Period = period,
            TimeSeriesPoints = { point },
            TimeSeriesType = TimeSeriesType.NetExchangePerGa,
            SettlementVersion = SettlementVersion.FirstCorrection.Name,
        };
    }

    private static void AssertProcessState(AggregatedMeasureDataProcess process, AggregatedMeasureDataProcess.State state)
    {
        var processState = typeof(AggregatedMeasureDataProcess).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(process);
        Assert.Equal(state, processState);
    }

    private static void AssertNumberOfPendingMessages(AggregatedMeasureDataProcess process, int numberOfMessages)
    {
        var pendingMessages = typeof(AggregatedMeasureDataProcess).GetField("_pendingMessages", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(process);
        Assert.Equal(numberOfMessages, pendingMessages!.GetType().GetProperty("Count")?.GetValue(pendingMessages));
    }

    private void AssertOutgoingMessageCreated(AggregatedMeasureDataProcess process, int expectedOutgoingMessages)
    {
        var outgoingMessages = _b2BContext.OutgoingMessages
            .ToList()
            .Where(x => x.ProcessId == process.ProcessId);
        Assert.NotNull(outgoingMessages);
        Assert.Equal(expectedOutgoingMessages, outgoingMessages.Count());
    }

    private void AssertSingleEnqueuedInternalCommand(string typeOfCommand)
    {
        var internalCommands = _b2BContext.QueuedInternalCommands
            .ToList()
            .Where(x => x.Type == typeOfCommand);
        Assert.NotNull(internalCommands);
        Assert.Single(internalCommands);
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(
        MarketRole roleOfReceiver,
        BusinessReason businessReason)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>());
    }

    private AggregatedMeasureDataProcess BuildProcess(MarketRole? receiverRole = null)
    {
        receiverRole ??= SampleData.BalanceResponsibleParty;

        var process = new AggregatedMeasureDataProcess(
          ProcessId.New(),
          BusinessTransactionId.Create(Guid.NewGuid().ToString()),
          SampleData.ReceiverNumber,
          receiverRole.Code,
          BusinessReason.Correction,
          MeteringPointType.Exchange.Code,
          null,
          SampleData.StartOfPeriod,
          SampleData.EndOfPeriod,
          SampleData.GridAreaCode,
          receiverRole == MarketRole.EnergySupplier ? SampleData.ReceiverNumber.Value : null,
          receiverRole == MarketRole.BalanceResponsibleParty ? SampleData.ReceiverNumber.Value : null,
          SettlementVersion.FirstCorrection);

        process.WasSentToWholesale();
        _b2BContext.AggregatedMeasureDataProcesses.Add(process);
        _b2BContext.SaveChanges();
        return process;
    }

    private AggregatedMeasureDataProcess? GetProcess(Guid processId)
    {
        _b2BContext.ChangeTracker.Clear();
        return _b2BContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.ProcessId.Id == processId);
    }
}
