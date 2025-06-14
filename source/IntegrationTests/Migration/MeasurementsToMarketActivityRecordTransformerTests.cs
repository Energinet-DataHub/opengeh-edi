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

using System.Text.Json;
using Energinet.DataHub.EDI.B2BApi.MeasurementsSynchronization;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime.Extensions;
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Migration;

public class MeasurementsToMarketActivityRecordTransformerTests
{
    [Fact]
    public void Given_Transform_When_ValidJson_Then_ReturnsValidMeteredDataForMeteringPointMarketActivityRecords()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var root = JsonSerializer.Deserialize<Root>(JsonPayloadConstants.SingleTimeSeriesWithSingleObservation, options) ?? throw new Exception("Root is null.");
        var timeSeries = root.MeteredDataTimeSeriesDH3.TimeSeries;
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var creationTime = header.Creation.ToInstant();

        // Act
        var actual = MeasurementsToMarketActivityRecordTransformer.Transform(creationTime, timeSeries);

        // Assert
        using var assertionScope = new AssertionScope();
        actual.Should().NotBeNull();
        actual.Should().ContainSingle();
        var record = actual[0];
        record.Should().BeOfType<MeteredDataForMeteringPointMarketActivityRecord>();
        record.MeteringPointId.Should().Be("571051839308770693");
        record.MeasurementUnit.Name.Should().Be(MeasurementUnit.KilowattHour.Name);
        record.MeteringPointType.Name.Should().Be(MeteringPointType.Consumption.Name);
        record.OriginalTransactionIdReference.Should().Be(null);
        record.Period.Start.Should().Be(InstantPattern.ExtendedIso.Parse("2023-12-25T23:00:00Z").Value);
        record.Period.End.Should().Be(InstantPattern.ExtendedIso.Parse("2023-12-26T23:00:00Z").Value);
        record.Product.Should().Be("8716867000030");
        record.RegistrationDateTime.Should().Be(InstantPattern.ExtendedIso.Parse("2024-01-16T07:55:33Z").Value);
        record.Resolution.Name.Should().Be(Resolution.Hourly.Name);
        record.TransactionId.Value.Should().Be("e1f06dee48d842c1a48b187065e710ff");
        record.Measurements.Single().Position.Should().Be(1);
        record.Measurements.Single().Quantity.Should().Be(2.0m);
        record.Measurements.Single().Quality!.Name.Should().Be(Quality.AsProvided.Name);
    }

    [Fact]
    public void Given_Transform_When_DeletedTimeSeries_Then_ReturnsEmptyList()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var root = JsonSerializer.Deserialize<Root>(JsonPayloadConstants.SingleDeletedTimeSeriesWithSingleObservation, options) ?? throw new Exception("Root is null.");
        var timeSeries = root.MeteredDataTimeSeriesDH3.TimeSeries;
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var creationTime = header.Creation.ToInstant();

        // Act
        var actual = MeasurementsToMarketActivityRecordTransformer.Transform(creationTime, timeSeries);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public void Given_Transform_When_ValidJsonWithMultipleTimeSeries_Then_ReturnsValidMeteredDataForMeteringPointMarketActivityRecords()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var root = JsonSerializer.Deserialize<Root>(JsonPayloadConstants.TwoTimeSeries, options) ?? throw new Exception("Root is null.");
        var timeSeries = root.MeteredDataTimeSeriesDH3.TimeSeries;
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var creationTime = header.Creation.ToInstant();

        // Act
        var actual = MeasurementsToMarketActivityRecordTransformer.Transform(creationTime, timeSeries);

        // Assert
        using var assertionScope = new AssertionScope();
        actual.Should().NotBeNull();
        actual.Should().HaveCount(2);
        var firstRecord = actual[0];
        firstRecord.TransactionId.Value.Should().Be("83521745ef4f4ada83f2115dda402e30");
        firstRecord.Measurements.Should().HaveCount(24);
    }

    [Fact]
    public void Given_Transform_When_CalledWithJsonContainingNoTs_Then_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var root = JsonSerializer.Deserialize<Root>(JsonPayloadConstants.NoTimeSeries, options) ?? throw new Exception("Root is null.");
        var timeSeries = root.MeteredDataTimeSeriesDH3.TimeSeries;
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var creationTime = header.Creation.ToInstant();

        // Act
        var act = () => MeasurementsToMarketActivityRecordTransformer.Transform(creationTime, timeSeries);

        // Assert
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Given_Transform_When_CalledWithQuantityMissingIndicatorTrueAndQualityNull_Then_ReturnsValidMeteredDataForMeteringPointMarketActivityRecord()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var root = JsonSerializer.Deserialize<Root>(
                       JsonPayloadConstants.QuantityMissingIndicatorTrueAndQualityNull, options) ?? throw new Exception("Root is null.");
        var timeSeries = root.MeteredDataTimeSeriesDH3.TimeSeries;
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var creationTime = header.Creation.ToInstant();

        // Act
        var actual = MeasurementsToMarketActivityRecordTransformer.Transform(creationTime, timeSeries);

        // Assert
        using var assertionScope = new AssertionScope();
        var actualRecord = actual[0];
        actualRecord.Measurements[0].Position.Should().Be(1);
        actualRecord.Measurements[0].Quantity.Should().Be(null);
        actualRecord.Measurements[0].Quality!.Name.Should().Be(Quality.NotAvailable.Name);
    }

    [Fact]
    public void Given_Transform_When_AllObservationsHaveQuantityMissingIndicatorTrue_Then_ReturnsEmptyList()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var root = JsonSerializer.Deserialize<Root>(
                       JsonPayloadConstants.QuantityMissingIndicatorTrueForAllObservations, options) ?? throw new Exception("Root is null.");
        var timeSeries = root.MeteredDataTimeSeriesDH3.TimeSeries;
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var creationTime = header.Creation.ToInstant();

        // Act
        var actual = MeasurementsToMarketActivityRecordTransformer.Transform(creationTime, timeSeries);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }
}
