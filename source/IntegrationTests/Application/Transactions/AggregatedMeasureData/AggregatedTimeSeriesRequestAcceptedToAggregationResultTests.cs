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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using Enum = System.Enum;
using Period = Energinet.DataHub.Edi.Responses.Period;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

public sealed class AggregatedTimeSeriesRequestAcceptedToAggregationResultTests : TestBase
{
    private static readonly QuantityQuality[][] _quantityQualityPowerSet = FastPowerSet(
        Enum.GetValues(typeof(QuantityQuality))
            .Cast<QuantityQuality>()
            .ToArray());

    private readonly GridAreaBuilder _gridAreaBuilder = new();
    private readonly ProcessContext _processContext;
    private readonly IInboxEventReceiver _inboxEventReceiver;
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly IMasterDataClient _masterDataClient;
    private readonly IFileStorageClient _fileStorageClient;

    public AggregatedTimeSeriesRequestAcceptedToAggregationResultTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processContext = GetService<ProcessContext>();
        _inboxEventReceiver = GetService<IInboxEventReceiver>();
        _databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        _masterDataClient = GetService<IMasterDataClient>();
        _fileStorageClient = GetService<IFileStorageClient>();
    }

    public static IEnumerable<object[]> QuantityQualityPowerSet()
    {
        return _quantityQualityPowerSet.Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithMissing()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(QuantityQuality.Missing))
            .Where(
                qqs => qqs.Length > 2
                       || (qqs.Length > 1 && !qqs.Contains(QuantityQuality.Unspecified)))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetOnlyMissing()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(QuantityQuality.Missing))
            .Where(
                qqs => qqs.Length == 1
                       || (qqs.Length == 2 && qqs.Contains(QuantityQuality.Unspecified)))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingWithEstimated()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(QuantityQuality.Estimated))
            .Where(qqs => !qqs.Contains(QuantityQuality.Missing))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingEstimatedWithMeasured()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(QuantityQuality.Measured))
            .Where(qqs => !qqs.Contains(QuantityQuality.Missing))
            .Where(qqs => !qqs.Contains(QuantityQuality.Estimated))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingEstimatedMeasuredWithCalculated()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(QuantityQuality.Calculated))
            .Where(qqs => !qqs.Contains(QuantityQuality.Missing))
            .Where(qqs => !qqs.Contains(QuantityQuality.Estimated))
            .Where(qqs => !qqs.Contains(QuantityQuality.Measured))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingEstimatedMeasuredCalculated()
    {
        return _quantityQualityPowerSet
            .Where(qqs => !qqs.Contains(QuantityQuality.Missing))
            .Where(qqs => !qqs.Contains(QuantityQuality.Estimated))
            .Where(qqs => !qqs.Contains(QuantityQuality.Measured))
            .Where(qqs => !qqs.Contains(QuantityQuality.Calculated))
            .Select(qqs => new object[] { qqs });
    }

    [Theory]
    [MemberData(nameof(QuantityQualityPowerSet))]
    public async Task AggregatedTimeSeriesRequestAccepted_with_power_set_values_of_quantity_quality_produces_Aggregation(
        QuantityQuality[] quantityQualities)
    {
        var result = await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, IReadOnlyCollection<AcceptedEnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, AcceptedEnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().BeDefined();
                });
    }

    [Theory]
    [InlineData(null, CalculatedQuantityQuality.NotAvailable)]
    [InlineData(QuantityQuality.Estimated, CalculatedQuantityQuality.Estimated)]
    [InlineData(QuantityQuality.Measured, CalculatedQuantityQuality.Measured)]
    [InlineData(QuantityQuality.Missing, CalculatedQuantityQuality.Missing)]
    [InlineData(QuantityQuality.Calculated, CalculatedQuantityQuality.Calculated)]
    public async Task Unspecified_quantity_quality_is_ignored_as_it_is_a_technical_artefact(
        QuantityQuality? quantityQuality,
        CalculatedQuantityQuality calculatedQuantityQuality)
    {
        var result = quantityQuality.HasValue
            ? await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(
                new[] { QuantityQuality.Unspecified, quantityQuality.Value })
            : await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(
                new[] { QuantityQuality.Unspecified });

        // Assert
        result
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, IReadOnlyCollection<AcceptedEnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, AcceptedEnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().Be(calculatedQuantityQuality);
                });
    }

    [Theory]
    [MemberData(nameof(QuantityQualityPowerSetOnlyMissing))]
    public async Task
        EnergyResultProducedV2_with_quantity_quality_Missing_produces_Aggregation_with_Missing_quantity_quality(
            QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, IReadOnlyCollection<AcceptedEnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, AcceptedEnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().Be(CalculatedQuantityQuality.Missing);
                });
    }

    [Theory]
    [MemberData(nameof(QuantityQualityPowerSetWithMissing))]
    public async Task
        EnergyResultProducedV2_with_quantity_quality_containing_Missing_produces_Aggregation_with_Incomplete_quantity_quality(
            QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, IReadOnlyCollection<AcceptedEnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, AcceptedEnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().Be(CalculatedQuantityQuality.Incomplete);
                });
    }

    [Theory]
    [MemberData(nameof(QuantityQualityPowerSetWithoutMissingWithEstimated))]
    public async Task
        EnergyResultProducedV2_with_quantity_quality_not_containing_Missing_but_contains_Estimated_produces_Aggregation_with_Estimated_quantity_quality(
            QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, IReadOnlyCollection<AcceptedEnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, AcceptedEnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().Be(CalculatedQuantityQuality.Estimated);
                });
    }

    [Theory]
    [MemberData(nameof(QuantityQualityPowerSetWithoutMissingEstimatedWithMeasured))]
    public async Task
        EnergyResultProducedV2_with_quantity_quality_not_containing_Missing_or_Estimated_but_contains_Measured_produces_Aggregation_with_Measured_quantity_quality(
            QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, IReadOnlyCollection<AcceptedEnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, AcceptedEnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().Be(CalculatedQuantityQuality.Measured);
                });
    }

    [Theory]
    [MemberData(nameof(QuantityQualityPowerSetWithoutMissingEstimatedMeasuredWithCalculated))]
    public async Task
        EnergyResultProducedV2_with_quantity_quality_not_containing_Missing_Estimated_or_Measured_but_contains_Calculated_produces_Aggregation_with_Calculated_quantity_quality(
            QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, IReadOnlyCollection<AcceptedEnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, AcceptedEnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().Be(CalculatedQuantityQuality.Calculated);
                });
    }

    [Theory]
    [MemberData(nameof(QuantityQualityPowerSetWithoutMissingEstimatedMeasuredCalculated))]
    public async Task
        EnergyResultProducedV2_with_quantity_quality_not_containing_Missing_Estimated_Measured_or_Calculated_produces_Aggregation_with_NotAvailable_quantity_quality(
            QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, IReadOnlyCollection<AcceptedEnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<AcceptedEnergyResultMessageTimeSeries, AcceptedEnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().Be(CalculatedQuantityQuality.NotAvailable);
                });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
    }

    private static AggregatedTimeSeriesRequestAccepted CreateEventWithQuantityQuality(
        IEnumerable<QuantityQuality> quantityQuality)
    {
        var timeSeriesPoint = new TimeSeriesPoint
        {
            Quantity = new DecimalValue { Units = 1, Nanos = 1 }, Time = new Timestamp { Seconds = 1, Nanos = 1 },
        };
        timeSeriesPoint.QuantityQualities.AddRange(quantityQuality);

        var series = new Series
        {
            GridArea = SampleData.GridAreaCode,
            TimeSeriesType = TimeSeriesType.Production,
            QuantityUnit = QuantityUnit.Kwh,
            Resolution = Resolution.Pt1H,
            Period = new Period()
            {
                StartOfPeriod = InstantPattern.General.Parse(SampleData.StartOfPeriod).Value.ToTimestamp(),
                EndOfPeriod = InstantPattern.General.Parse(SampleData.EndOfPeriod).Value.ToTimestamp(),
            },
        };
        series.TimeSeriesPoints.Add(timeSeriesPoint);

        var requestAccepted = new AggregatedTimeSeriesRequestAccepted();
        requestAccepted.Series.Add(series);

        return requestAccepted;
    }

    // Taken from https://stackoverflow.com/questions/19890781/creating-a-power-set-of-a-sequence
    // by user https://stackoverflow.com/users/1740808/sergeys
    private static T[][] FastPowerSet<T>(IReadOnlyList<T> seq)
    {
        var powerSet = new T[1 << seq.Count][];
        powerSet[0] = Array.Empty<T>(); // starting only with empty set

        for (var i = 0; i < seq.Count; i++)
        {
            var cur = seq[i];
            var count = 1 << i; // doubling list each time
            for (var j = 0; j < count; j++)
            {
                var source = powerSet[j];
                var destination = powerSet[count + j] = new T[source.Length + 1];
                for (var q = 0; q < source.Length; q++)
                    destination[q] = source[q];
                destination[source.Length] = cur;
            }
        }

        return powerSet;
    }

    private async Task<AssertOutgoingMessage> AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(
        IEnumerable<QuantityQuality> quantityQuality)
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .StoreAsync(_masterDataClient);

        var process = await BuildProcess();
        var requestAccepted = CreateEventWithQuantityQuality(quantityQuality);

        var eventId = EventId.From(Guid.NewGuid());
        await _inboxEventReceiver
            .ReceiveAsync(
                eventId,
                nameof(AggregatedTimeSeriesRequestAccepted),
                process.ProcessId.Id,
                requestAccepted.ToByteArray());

        await HavingReceivedInboxEventAsync(
            nameof(AggregatedTimeSeriesRequestAccepted),
            requestAccepted,
            process.ProcessId.Id,
            eventId.Value);

        return await OutgoingMessageAsync();
    }

    private async Task<AggregatedMeasureDataProcess> BuildProcess()
    {
        var requestedByActor = RequestedByActor.From(SampleData.ReceiverNumber, SampleData.BalanceResponsibleParty);

        var process = new AggregatedMeasureDataProcess(
            ProcessId.New(),
            requestedByActor,
            OriginalActor.From(requestedByActor),
            TransactionId.New(),
            BusinessReason.BalanceFixing,
            MessageId.New(),
            MeteringPointType.Production.Code,
            null,
            SampleData.StartOfPeriod,
            SampleData.EndOfPeriod,
            SampleData.GridAreaCode,
            null,
            SampleData.ReceiverNumber.Value,
            null,
            new[] { SampleData.GridAreaCode });

        process.SendToWholesale();
        _processContext.AggregatedMeasureDataProcesses.Add(process);
        await _processContext.SaveChangesAsync();
        return process;
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync()
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            BusinessReason.BalanceFixing.Name,
            ActorRole.BalanceResponsibleParty,
            _databaseConnectionFactory,
            _fileStorageClient);
    }
}
