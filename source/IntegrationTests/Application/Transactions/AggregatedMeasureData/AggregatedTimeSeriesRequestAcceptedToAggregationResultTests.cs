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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Categories;
using Enum = System.Enum;
using Point = Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage.Point;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1204:Static elements should appear before instance elements",
    Justification = "Test class")]
public sealed class AggregatedTimeSeriesRequestAcceptedToAggregationResultTests : TestBase
{
    private static readonly QuantityQuality[][] _quantityQualityPowerSet = FastPowerSet(
        Enum.GetValues(typeof(QuantityQuality))
            .Cast<QuantityQuality>()
            .ToArray());

    private readonly GridAreaBuilder _gridAreaBuilder = new();
    private readonly ProcessContext _processContext;
    private readonly InboxEventReceiver _inboxEventReceiver;
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly IMasterDataClient _masterDataClient;
    private readonly IFileStorageClient _fileStorageClient;

    public AggregatedTimeSeriesRequestAcceptedToAggregationResultTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _processContext = GetService<ProcessContext>();
        _inboxEventReceiver = GetService<InboxEventReceiver>();
        _databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        _masterDataClient = GetService<IMasterDataClient>();
        _fileStorageClient = GetService<IFileStorageClient>();
    }

    public static IEnumerable<object[]> QuantityQualityPowerSet()
    {
        return _quantityQualityPowerSet.Select(x => new object[] { x });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithMissing()
    {
        return _quantityQualityPowerSet
            .Where(x => x.Contains(QuantityQuality.Missing))
            .Where(
                x => x.Length > 2
                     || (x.Length > 1 && !x.Contains(QuantityQuality.Unspecified)))
            .Select(x => new object[] { x });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetOnlyMissing()
    {
        return _quantityQualityPowerSet
            .Where(x => x.Contains(QuantityQuality.Missing))
            .Where(
                x => x.Length == 1
                     || (x.Length == 2 && x.Contains(QuantityQuality.Unspecified)))
            .Select(x => new object[] { x });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingWithEstimated()
    {
        return _quantityQualityPowerSet
            .Where(x => x.Contains(QuantityQuality.Estimated))
            .Where(x => !x.Contains(QuantityQuality.Missing))
            .Select(x => new object[] { x });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingEstimatedWithMeasured()
    {
        return _quantityQualityPowerSet
            .Where(x => x.Contains(QuantityQuality.Measured))
            .Where(x => !x.Contains(QuantityQuality.Missing))
            .Where(x => !x.Contains(QuantityQuality.Estimated))
            .Select(x => new object[] { x });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingEstimatedMeasuredWithCalculated()
    {
        return _quantityQualityPowerSet
            .Where(x => x.Contains(QuantityQuality.Calculated))
            .Where(x => !x.Contains(QuantityQuality.Missing))
            .Where(x => !x.Contains(QuantityQuality.Estimated))
            .Where(x => !x.Contains(QuantityQuality.Measured))
            .Select(x => new object[] { x });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingEstimatedMeasuredCalculated()
    {
        return _quantityQualityPowerSet
            .Where(x => !x.Contains(QuantityQuality.Missing))
            .Where(x => !x.Contains(QuantityQuality.Estimated))
            .Where(x => !x.Contains(QuantityQuality.Measured))
            .Where(x => !x.Contains(QuantityQuality.Calculated))
            .Select(x => new object[] { x });
    }

    [Theory]
    [MemberData(nameof(QuantityQualityPowerSet))]
    public async Task EnergyResultProducedV2_with_power_set_values_of_quantity_quality_produces_Aggregation(
        QuantityQuality[] quantityQualities)
    {
        var result = await AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<TimeSeries, IReadOnlyList<Point>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<TimeSeries, Point>(
                series => series.Point[0],
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
            .HasMessageRecordValue<TimeSeries, IReadOnlyList<Point>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<TimeSeries, Point>(
                series => series.Point[0],
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
            .HasMessageRecordValue<TimeSeries, IReadOnlyList<Point>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<TimeSeries, Point>(
                series => series.Point[0],
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
            .HasMessageRecordValue<TimeSeries, IReadOnlyList<Point>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<TimeSeries, Point>(
                series => series.Point[0],
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
            .HasMessageRecordValue<TimeSeries, IReadOnlyList<Point>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<TimeSeries, Point>(
                series => series.Point[0],
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
            .HasMessageRecordValue<TimeSeries, IReadOnlyList<Point>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<TimeSeries, Point>(
                series => series.Point[0],
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
            .HasMessageRecordValue<TimeSeries, IReadOnlyList<Point>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<TimeSeries, Point>(
                series => series.Point[0],
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
            .HasMessageRecordValue<TimeSeries, IReadOnlyList<Point>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<TimeSeries, Point>(
                series => series.Point[0],
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

    private async Task<AssertOutgoingMessage> AggregatedTimeSeriesRequestAcceptedWithQuantityQualityToOutgoingMessage(
        IEnumerable<QuantityQuality> quantityQuality)
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .StoreAsync(_masterDataClient);

        var process = BuildProcess();
        var requestAccepted = CreateEventWithQuantityQuality(quantityQuality);

        await _inboxEventReceiver
            .ReceiveAsync(
                Guid.NewGuid().ToString(),
                nameof(AggregatedTimeSeriesRequestAccepted),
                process.ProcessId.Id,
                requestAccepted.ToByteArray());

        await HavingReceivedInboxEventAsync(
            nameof(AggregatedTimeSeriesRequestAccepted),
            requestAccepted,
            process.ProcessId.Id);

        return await OutgoingMessageAsync();
    }

    private static AggregatedTimeSeriesRequestAccepted CreateEventWithQuantityQuality(IEnumerable<QuantityQuality> quantityQuality)
    {
        var timeSeriesPoint = new TimeSeriesPoint
        {
            Quantity = new DecimalValue { Units = 1, Nanos = 1 },
            Time = new Timestamp { Seconds = 1, Nanos = 1 },
        };
        timeSeriesPoint.QuantityQuality.AddRange(quantityQuality);

        var series = new Series
        {
            GridArea = SampleData.GridAreaCode,
            TimeSeriesType = TimeSeriesType.Production,
            QuantityUnit = QuantityUnit.Kwh,
            Resolution = Resolution.Pt1H,
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

    private AggregatedMeasureDataProcess BuildProcess()
    {
        var process = new AggregatedMeasureDataProcess(
            ProcessId.New(),
            BusinessTransactionId.Create(Guid.NewGuid().ToString()),
            SampleData.ReceiverNumber,
            SampleData.BalanceResponsibleParty.Code,
            BusinessReason.BalanceFixing,
            MeteringPointType.Production.Code,
            null,
            SampleData.StartOfPeriod,
            SampleData.EndOfPeriod,
            SampleData.GridAreaCode,
            null,
            SampleData.ReceiverNumber.Value,
            null);

        process.WasSentToWholesale();
        _processContext.AggregatedMeasureDataProcesses.Add(process);
        _processContext.SaveChanges();
        return process;
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync()
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            BusinessReason.BalanceFixing.Name,
            MarketRole.BalanceResponsibleParty,
            _databaseConnectionFactory,
            _fileStorageClient);
    }
}
