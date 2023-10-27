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
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NodaTime.Text;
using Xunit;
using Xunit.Categories;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class WhenAnAcceptedResultIsAvailableTests : TestBase
{
    private readonly B2BContext _b2BContext;
    private readonly GridAreaBuilder _gridAreaBuilder = new();

    public WhenAnAcceptedResultIsAvailableTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
    }

    [Fact]
    public async Task Aggregated_measure_data_response_is_accepted()
    {
        // Arrange
        _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .Store(_b2BContext);
        var process = BuildProcess();
        var acceptedEvent = GetAcceptedEvent(process);

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(MarketRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);

        outgoingMessage
            .HasBusinessReason(process.BusinessReason)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(MarketRole.FromCode(process.RequestedByActorRoleCode).Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(timeSerie => timeSerie.BalanceResponsibleNumber, process.BalanceResponsibleId)
            .HasMessageRecordValue<TimeSeries>(timeSerie => timeSerie.EnergySupplierNumber, process.EnergySupplierId);
    }

    [Fact]
    public async Task Received_2_accepted_events_for_same_aggregated_measure_data_process()
    {
        // Arrange
        _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .Store(_b2BContext);
        var process = BuildProcess();
        var acceptedEvent = GetAcceptedEvent(process);

        // Act
        await AddInboxEvent(process, acceptedEvent);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(MarketRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);

        outgoingMessage
            .HasBusinessReason(process.BusinessReason)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(MarketRole.FromCode(process.RequestedByActorRoleCode).Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _b2BContext.Dispose();
    }

    private static AggregatedTimeSeriesRequestAccepted GetAcceptedEvent(AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
    {
        return CreateAggregation(aggregatedMeasureDataProcess);
    }

    private static AggregatedTimeSeriesRequestAccepted CreateAggregation(AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
    {
        var quantity = new DecimalValue() { Units = 12345, Nanos = 123450000, };
        var point = new TimeSeriesPoint()
        {
            Quantity = quantity,
            QuantityQuality = QuantityQuality.Incomplete,
            Time = new Timestamp() { Seconds = 1, },
        };

        return new AggregatedTimeSeriesRequestAccepted()
        {
            GridArea = aggregatedMeasureDataProcess.MeteringGridAreaDomainId,
            QuantityUnit = QuantityUnit.Kwh,
            TimeSeriesPoints = { point },
            TimeSeriesType = TimeSeriesType.Production,
            Resolution = Resolution.Pt15M,
        };
    }

    private async Task AddInboxEvent(
        AggregatedMeasureDataProcess process,
        AggregatedTimeSeriesRequestAccepted acceptedEvent)
    {
        await GetService<InboxEventReceiver>()
            .ReceiveAsync(
                Guid.NewGuid().ToString(),
                nameof(AggregatedTimeSeriesRequestAccepted),
                process.ProcessId.Id,
                acceptedEvent.ToByteArray());
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
          BusinessReason.BalanceFixing,
          MeteringPointType.Production.Code,
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
}
