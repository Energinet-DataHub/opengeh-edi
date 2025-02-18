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

using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Google.Protobuf;
using NodaTime;
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
using SettlementVersion = Energinet.DataHub.Edi.Responses.SettlementVersion;
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
        await AddInboxEvent(process, acceptedEvent);
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestAccepted), acceptedEvent, process.ProcessId.Id);

        // Assert
        var outgoingMessage = await OutgoingMessagesAsync(ActorRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);
        outgoingMessage.Should().ContainSingle();
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
            currentTime = currentTime.Plus(Duration.FromMinutes(15));
        }

        var series = new Series
        {
            GridArea = aggregatedMeasureDataProcess.RequestedGridArea,
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

        if (aggregatedMeasureDataProcess.BusinessReason.Name == BusinessReason.Correction.Name)
        {
            switch (aggregatedMeasureDataProcess.SettlementVersion)
            {
                case var first when first == BuildingBlocks.Domain.Models.SettlementVersion.FirstCorrection:
                    series.SettlementVersion = SettlementVersion.FirstCorrection;
                    break;
                case var second when second == BuildingBlocks.Domain.Models.SettlementVersion.SecondCorrection:
                    series.SettlementVersion = SettlementVersion.SecondCorrection;
                    break;
                default:
                    series.SettlementVersion = SettlementVersion.ThirdCorrection;
                    break;
            }
        }

        var aggregatedTimeSeries = new AggregatedTimeSeriesRequestAccepted();
        aggregatedTimeSeries.Series.Add(series);

        return aggregatedTimeSeries;
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

    private async Task<AggregatedMeasureDataProcess> BuildProcess(ActorRole? receiverRole = null, BusinessReason? businessReason = null)
    {
        receiverRole ??= SampleData.BalanceResponsibleParty;

        var requestedByActor = RequestedByActor.From(SampleData.ReceiverNumber, receiverRole);

        var process = new AggregatedMeasureDataProcess(
          ProcessId.New(),
          requestedByActor,
          OriginalActor.From(requestedByActor),
          TransactionId.New(),
          businessReason ?? BusinessReason.BalanceFixing,
          MessageId.New(),
          MeteringPointType.Consumption.Code,
          SettlementMethod.Flex.Code,
          SampleData.StartOfPeriod,
          SampleData.EndOfPeriod,
          SampleData.GridAreaCode,
          receiverRole == ActorRole.EnergySupplier ? SampleData.ReceiverNumber.Value : null,
          receiverRole == ActorRole.BalanceResponsibleParty ? SampleData.ReceiverNumber.Value : null,
          null,
          new[] { SampleData.GridAreaCode });

        process.SendToWholesale();
        _processContext.AggregatedMeasureDataProcesses.Add(process);
        await _processContext.SaveChangesAsync();
        return process;
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
            $" WHERE m.DocumentType = '{DocumentType.NotifyAggregatedMeasureData.Name}' AND m.BusinessReason = '{businessReason.Name}' AND m.ReceiverRole = '{receiverRole.Code}'");

        return messages.ToList().AsReadOnly();
    }
}
