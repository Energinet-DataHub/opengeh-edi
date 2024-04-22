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
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Google.Protobuf;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;
using DecimalValue = Energinet.DataHub.Edi.Responses.DecimalValue;
using Period = Energinet.DataHub.Edi.Responses.Period;
using QuantityQuality = Energinet.DataHub.Edi.Responses.QuantityQuality;
using QuantityUnit = Energinet.DataHub.Edi.Responses.QuantityUnit;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;
using TimeSeriesPoint = Energinet.DataHub.Edi.Responses.TimeSeriesPoint;
using TimeSeriesType = Energinet.DataHub.Edi.Responses.TimeSeriesType;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class WhenAnAcceptedResultIsAvailableTests : TestBase
{
    private readonly GridAreaBuilder _gridAreaBuilder = new();
    private readonly ProcessContext _processContext;

    public WhenAnAcceptedResultIsAvailableTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processContext = GetService<ProcessContext>();
    }

    [Fact]
    public async Task Aggregated_measure_data_response_is_accepted() // TODO: Shouldn't we have a test with 2 series in the same accepted event, which creates 2 outgoing messages?
    {
        // Arrange
        var expectedEventId = "expected-event-id";
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .StoreAsync(GetService<IMasterDataClient>());

        var process = await BuildProcess();
        var acceptedEvent = GetAcceptedEvent(process);

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestAccepted), acceptedEvent, process.ProcessId.Id, expectedEventId);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(ActorRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);

        outgoingMessage
            .HasProcessId(process.ProcessId)
            .HasEventId(expectedEventId)
            .HasBusinessReason(process.BusinessReason)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(process.RequestedByActorRoleCode)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasProcessType(ProcessType.RequestEnergyResults)
            .HasRelationTo(process.InitiatedByMessageId)
            .HasGridAreaCode(acceptedEvent.Series.Single().GridArea)
            .HasPointsInCorrectOrder<AcceptedEnergyResultMessageTimeSeries, decimal?>(timeSerie => timeSerie.Point.Select(x => x.Quantity).ToList(), acceptedEvent.Series.SelectMany(x => x.TimeSeriesPoints).OrderBy(x => x.Time).ToList())
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries>(timeSerie => timeSerie.BalanceResponsibleNumber, process.BalanceResponsibleId)
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries>(timeSerie => timeSerie.EnergySupplierNumber, process.EnergySupplierId)
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries>(timeSerie => timeSerie.CalculationResultVersion, 1)
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries>(timeSerie => timeSerie.OriginalTransactionIdReference, process.BusinessTransactionId.Id);
    }

    [Fact]
    public async Task Received_2_accepted_events_for_same_aggregated_measure_data_process()
    {
        // Arrange
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .StoreAsync(GetService<IMasterDataClient>());

        var process = await BuildProcess();
        var acceptedEvent = GetAcceptedEvent(process);

        // Act
        await AddInboxEvent(process, acceptedEvent);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(ActorRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);

        outgoingMessage
            .HasBusinessReason(process.BusinessReason)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(process.RequestedByActorRoleCode)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries>(timeSerie => timeSerie.CalculationResultVersion, 1);
    }

    [Fact]
    public async Task Given_AcceptedInboxEventWithTwoSeries_When_ReceivingInboxEvent_Then_EachOutgoingMessageHasAUniqueTransactionId()
    {
        // Arrange
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .StoreAsync(GetService<IMasterDataClient>());

        var process = await BuildProcess();
        var acceptedEvent = GetAcceptedEvent(process);
        acceptedEvent.Series.Add(acceptedEvent.Series.First());

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessages = await AllOutgoingMessageAsync(ActorRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);
        outgoingMessages.Count.Should().Be(2);
        var firstMessage = outgoingMessages.First();
        var secondMessage = outgoingMessages.Last();

        var seriesIdOfFirstMessage = firstMessage.GetMessageValue<AcceptedEnergyResultMessageTimeSeries, Guid>(series => series.TransactionId);
        var seriesIdOfSecondMessage = secondMessage.GetMessageValue<AcceptedEnergyResultMessageTimeSeries, Guid>(series => series.TransactionId);

        seriesIdOfFirstMessage.Should().NotBe(seriesIdOfSecondMessage);
    }

    [Fact]
    public async Task Received_accepted_events_with_multiple_point_ensure_ordering_is_correct()
    {
        // Arrange
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .StoreAsync(GetService<IMasterDataClient>());

        var process = await BuildProcess();
        var acceptedEvent = GetAcceptedEvent(process);

        // Act
        await AddInboxEvent(process, acceptedEvent);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(ActorRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);

        outgoingMessage
            .HasBusinessReason(process.BusinessReason)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(process.RequestedByActorRoleCode)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries>(timeSerie => timeSerie.CalculationResultVersion, 1);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
    }

    private static AggregatedTimeSeriesRequestAccepted GetAcceptedEvent(AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
    {
        return CreateAggregation(aggregatedMeasureDataProcess);
    }

    private static AggregatedTimeSeriesRequestAccepted CreateAggregation(AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
    {
        List<TimeSeriesPoint> timeSeriesPoints = new();
        var currentTime = InstantPattern.General.Parse(aggregatedMeasureDataProcess.StartOfPeriod).Value;
        while (currentTime < InstantPattern.General.Parse(aggregatedMeasureDataProcess.EndOfPeriod!).Value)
        {
            var quantity = new DecimalValue() { Units = currentTime.ToUnixTimeSeconds(), Nanos = 123450000, };
            timeSeriesPoints.Add(new TimeSeriesPoint(new TimeSeriesPoint()
            {
                Quantity = quantity,
                Time = currentTime.ToTimestamp(),
                QuantityQualities = { QuantityQuality.Calculated },
            }));
            currentTime = currentTime.Plus(NodaTime.Duration.FromMinutes(15));
        }

        var series = new Series
        {
            GridArea = aggregatedMeasureDataProcess.MeteringGridAreaDomainId,
            QuantityUnit = QuantityUnit.Kwh,
            TimeSeriesType = TimeSeriesType.Production,
            Resolution = Resolution.Pt15M,
            CalculationResultVersion = 1,
            Period = new Period()
            {
                StartOfPeriod = InstantPattern.General.Parse(aggregatedMeasureDataProcess.StartOfPeriod).Value.ToTimestamp(),
                EndOfPeriod = InstantPattern.General.Parse(aggregatedMeasureDataProcess.EndOfPeriod!).Value.ToTimestamp(),
            },
        };
        series.TimeSeriesPoints.AddRange(timeSeriesPoints.OrderBy(_ => Guid.NewGuid()));
        var aggregatedTimeSerie = new AggregatedTimeSeriesRequestAccepted();
        aggregatedTimeSerie.Series.Add(series);

        return aggregatedTimeSerie;
    }

    private async Task AddInboxEvent(
        AggregatedMeasureDataProcess process,
        AggregatedTimeSeriesRequestAccepted acceptedEvent)
    {
        await GetService<IInboxEventReceiver>()
            .ReceiveAsync(
                EventId.From(Guid.NewGuid()),
                nameof(AggregatedTimeSeriesRequestAccepted),
                process.ProcessId.Id,
                acceptedEvent.ToByteArray());
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(
        ActorRole roleOfReceiver,
        BusinessReason businessReason)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
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
            DocumentType.NotifyAggregatedMeasureData.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>(),
            GetService<IFileStorageClient>());
    }

    private async Task<AggregatedMeasureDataProcess> BuildProcess(ActorRole? receiverRole = null)
    {
        receiverRole ??= SampleData.BalanceResponsibleParty;

        var process = new AggregatedMeasureDataProcess(
          ProcessId.New(),
          BusinessTransactionId.Create(Guid.NewGuid().ToString()),
          SampleData.ReceiverNumber,
          receiverRole.Code,
          BusinessReason.BalanceFixing,
          MessageId.New(),
          MeteringPointType.Consumption.Code,
          SettlementMethod.Flex.Code,
          SampleData.StartOfPeriod,
          SampleData.EndOfPeriod,
          SampleData.GridAreaCode,
          receiverRole == ActorRole.EnergySupplier ? SampleData.ReceiverNumber.Value : null,
          receiverRole == ActorRole.BalanceResponsibleParty ? SampleData.ReceiverNumber.Value : null,
          null);

        process.SendToWholesale();
        _processContext.AggregatedMeasureDataProcesses.Add(process);
        await _processContext.SaveChangesAsync();
        return process;
    }
}
