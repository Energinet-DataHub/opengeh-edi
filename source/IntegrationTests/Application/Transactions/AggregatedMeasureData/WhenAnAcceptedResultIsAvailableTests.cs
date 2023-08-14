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
using System.Linq;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Configuration.DataAccess;
using Infrastructure.InboxEvents;
using Infrastructure.OutgoingMessages.Common;
using IntegrationTests.Assertions;
using IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
using Period = Energinet.DataHub.Edi.Responses.Period;

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class WhenAnAcceptedResultIsAvailableTests : TestBase
{
    private readonly B2BContext _b2BContext;

    public WhenAnAcceptedResultIsAvailableTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
    }

    [Fact]
    public async Task Aggregated_measure_data_response_is_accepted()
    {
        // Arrange
        var process = BuildProcess();
        var acceptedEvent = GetAcceptedEvent(process);

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestAccepted), acceptedEvent, process.ProcessId.Id).ConfigureAwait(false);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(MarketRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);
        var timeSerie = acceptedEvent.Series.First();
        outgoingMessage
            .HasBusinessReason(CimCode.To(process.BusinessReason).Name)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(MarketRole.FromCode(process.RequestedByActorRoleCode).Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(timeSerie => timeSerie.SettlementVersion!, timeSerie.SettlementVersion);
    }

    [Fact]
    public async Task Received_2_accepted_events_for_same_aggregated_measure_data_process()
    {
        // Arrange
        var process = BuildProcess();
        var acceptedEvent = GetAcceptedEvent(process);

        // Act
        await GetService<InboxEventReceiver>()
            .ReceiveAsync(Guid.NewGuid().ToString(), nameof(AggregatedTimeSeriesRequestAccepted), process.ProcessId.Id, acceptedEvent.ToByteArray()).ConfigureAwait(false);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(MarketRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);
        var timeSerie = acceptedEvent.Series.First();
        outgoingMessage
            .HasBusinessReason(CimCode.To(process.BusinessReason).Name)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(MarketRole.FromCode(process.RequestedByActorRoleCode).Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(timeSerie => timeSerie.SettlementVersion!, timeSerie.SettlementVersion);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _b2BContext.Dispose();
    }

    private static AggregatedTimeSeriesRequestAccepted GetAcceptedEvent(AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
    {
        var acceptedResponse = new AggregatedTimeSeriesRequestAccepted();
        acceptedResponse.Series.Add(CreateSerie(aggregatedMeasureDataProcess));

        return acceptedResponse;
    }

    private static Serie CreateSerie(AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
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
            StartOfPeriod = new Timestamp() { Seconds = aggregatedMeasureDataProcess.StartOfPeriod.ToUnixTimeSeconds(), },
            EndOfPeriod = new Timestamp() { Seconds = aggregatedMeasureDataProcess.EndOfPeriod?.ToUnixTimeSeconds() ?? 1, },
            Resolution = Resolution.Pt15M,
        };

        return new Serie()
        {
#pragma warning disable CA1305
            SettlementVersion = aggregatedMeasureDataProcess.SettlementVersion ?? "0",
#pragma warning restore CA1305
            GridArea = aggregatedMeasureDataProcess.MeteringGridAreaDomainId,
            Product = Product.Tarif,
            QuantityUnit = QuantityUnit.Kwh,
            Period = period,
            TimeSeriesPoints = { point },
            TimeSeriesType = TimeSeriesType.Production,
        };
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(
        MarketRole roleOfReceiver,
        BusinessReason businessReason)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
    }

    private AggregatedMeasureDataProcess BuildProcess()
    {
        var process = new AggregatedMeasureDataProcess(
          ProcessId.New(),
          BusinessTransactionId.Create(Guid.NewGuid().ToString()),
          SampleData.Receiver,
          SampleData.ReceiverRole.Code,
          CimCode.Of(BusinessReason.BalanceFixing),
          null,
          null,
          null,
          SampleData.StartOfPeriod,
          SampleData.EndOfPeriod,
          SampleData.GridAreaCode,
          null,
          null);

        process.WasSentToWholesale();
        _b2BContext.AggregatedMeasureDataProcesses.Add(process);
        _b2BContext.SaveChanges();
        return process;
    }
}
