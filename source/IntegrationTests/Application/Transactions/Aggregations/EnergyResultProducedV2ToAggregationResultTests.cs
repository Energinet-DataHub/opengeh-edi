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
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Enum = System.Enum;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.Aggregations;

public sealed class EnergyResultProducedV2ToAggregationResultTests : TestBase
{
    private static readonly EnergyResultProducedV2.Types.QuantityQuality[][] _quantityQualityPowerSet = FastPowerSet(
        Enum.GetValues(typeof(EnergyResultProducedV2.Types.QuantityQuality))
            .Cast<EnergyResultProducedV2.Types.QuantityQuality>()
            .ToArray());

    private readonly IMasterDataClient _masterDataClient;
    private readonly IIntegrationEventHandler _integrationEventHandler;
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

    private readonly EnergyResultProducedV2EventBuilder _eventBuilder = new();
    private readonly GridAreaBuilder _gridAreaBuilder = new();
    private readonly IFileStorageClient _fileStorageClient;

    public EnergyResultProducedV2ToAggregationResultTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _masterDataClient = GetService<IMasterDataClient>();
        _integrationEventHandler = GetService<IIntegrationEventHandler>();
        _databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        _fileStorageClient = GetService<IFileStorageClient>();
    }

    public static IEnumerable<object[]> QuantityQualityPowerSet()
    {
        return _quantityQualityPowerSet.Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithMissing()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Missing))
            .Where(
                qqs => qqs.Length > 2
                       || (qqs.Length > 1 && !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Unspecified)))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetOnlyMissing()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Missing))
            .Where(
                qqs => qqs.Length == 1
                       || (qqs.Length == 2 && qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Unspecified)))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingWithEstimated()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Estimated))
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Missing))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingEstimatedWithMeasured()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Measured))
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Missing))
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Estimated))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingEstimatedMeasuredWithCalculated()
    {
        return _quantityQualityPowerSet
            .Where(qqs => qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Calculated))
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Missing))
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Estimated))
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Measured))
            .Select(qqs => new object[] { qqs });
    }

    public static IEnumerable<object[]> QuantityQualityPowerSetWithoutMissingEstimatedMeasuredCalculated()
    {
        return _quantityQualityPowerSet
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Missing))
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Estimated))
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Measured))
            .Where(qqs => !qqs.Contains(EnergyResultProducedV2.Types.QuantityQuality.Calculated))
            .Select(qqs => new object[] { qqs });
    }

    [Theory]
    [MemberData(nameof(QuantityQualityPowerSet))]
    public async Task EnergyResultProducedV2_with_power_set_values_of_quantity_quality_produces_Aggregation(
        EnergyResultProducedV2.Types.QuantityQuality[] quantityQualities)
    {
        var result = await EnergyResultToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, IReadOnlyCollection<EnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, EnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().BeDefined();
                });
    }

    [Theory]
    [InlineData(null, CalculatedQuantityQuality.NotAvailable)]
    [InlineData(EnergyResultProducedV2.Types.QuantityQuality.Estimated, CalculatedQuantityQuality.Estimated)]
    [InlineData(EnergyResultProducedV2.Types.QuantityQuality.Measured, CalculatedQuantityQuality.Measured)]
    [InlineData(EnergyResultProducedV2.Types.QuantityQuality.Missing, CalculatedQuantityQuality.Missing)]
    [InlineData(EnergyResultProducedV2.Types.QuantityQuality.Calculated, CalculatedQuantityQuality.Calculated)]
    public async Task Unspecified_quantity_quality_is_ignored_as_it_is_a_technical_artefact(
        EnergyResultProducedV2.Types.QuantityQuality? quantityQuality,
        CalculatedQuantityQuality calculatedQuantityQuality)
    {
        var result = quantityQuality.HasValue
            ? await EnergyResultToOutgoingMessage(
                new[] { EnergyResultProducedV2.Types.QuantityQuality.Unspecified, quantityQuality.Value })
            : await EnergyResultToOutgoingMessage(
                new[] { EnergyResultProducedV2.Types.QuantityQuality.Unspecified });

        // Assert
        result
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, IReadOnlyCollection<EnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, EnergyResultMessagePoint>(
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
            EnergyResultProducedV2.Types.QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await EnergyResultToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, IReadOnlyCollection<EnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, EnergyResultMessagePoint>(
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
            EnergyResultProducedV2.Types.QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await EnergyResultToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, IReadOnlyCollection<EnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, EnergyResultMessagePoint>(
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
            EnergyResultProducedV2.Types.QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await EnergyResultToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, IReadOnlyCollection<EnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, EnergyResultMessagePoint>(
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
            EnergyResultProducedV2.Types.QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await EnergyResultToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, IReadOnlyCollection<EnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, EnergyResultMessagePoint>(
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
            EnergyResultProducedV2.Types.QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await EnergyResultToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, IReadOnlyCollection<EnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, EnergyResultMessagePoint>(
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
            EnergyResultProducedV2.Types.QuantityQuality[] quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities, nameof(quantityQualities));

        var result = await EnergyResultToOutgoingMessage(quantityQualities);

        // Assert
        result
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, IReadOnlyCollection<EnergyResultMessagePoint>>(
                series => series.Point,
                points => points.Should().ContainSingle())
            .HasMessageRecordValue<EnergyResultMessageTimeSeries, EnergyResultMessagePoint>(
                series => series.Point.First(),
                point =>
                {
                    point.Should().NotBeNull();
                    point!.QuantityQuality.Should().Be(CalculatedQuantityQuality.NotAvailable);
                });
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

    private async Task<AssertOutgoingMessage> EnergyResultToOutgoingMessage(
        IEnumerable<EnergyResultProducedV2.Types.QuantityQuality> quantityQualities)
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .StoreAsync(_masterDataClient);

        var energyResultProducedV2 = _eventBuilder
            .AggregatedBy(SampleData.GridAreaCode)
            .WithQuantityQualities(quantityQualities)
            .Build();

        var integrationEvent = new IntegrationEvent(
            Guid.NewGuid(),
            EnergyResultProducedV2.EventName,
            1,
            energyResultProducedV2);
        await _integrationEventHandler.HandleAsync(integrationEvent);

        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            BusinessReason.BalanceFixing.Name,
            ActorRole.MeteredDataResponsible,
            _databaseConnectionFactory,
            _fileStorageClient);
    }
}
