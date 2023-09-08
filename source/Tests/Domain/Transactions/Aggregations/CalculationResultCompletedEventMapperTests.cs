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
using Energinet.DataHub.Wholesale.Contracts.Events;
using Xunit;

namespace Tests.Domain.Transactions.Aggregations;

public class CalculationResultCompletedEventMapperTests
{
    public static IEnumerable<object[]> ProcessTypes()
    {
        foreach (var number in Enum.GetValues(typeof(ProcessType)))
        {
            yield return new[] { number };
        }
    }

    public static IEnumerable<object[]> QuantityQualities()
    {
        foreach (var number in Enum.GetValues(typeof(QuantityQuality)))
        {
            yield return new[] { number };
        }
    }

    public static IEnumerable<object[]> Resolutions()
    {
        foreach (var number in Enum.GetValues(typeof(Resolution)))
        {
            yield return new[] { number };
        }
    }

    public static IEnumerable<object[]> QuantityUnits()
    {
        foreach (var number in Enum.GetValues(typeof(QuantityUnit)))
        {
            yield return new[] { number };
        }
    }

    public static IEnumerable<object[]> TimeSeriesTypes()
    {
        foreach (var number in Enum.GetValues(typeof(TimeSeriesType)))
        {
            yield return new[] { number };
        }
    }

    [Theory]
    [MemberData(nameof(ProcessTypes))]
    public void Ensure_handling_all_process_types(ProcessType processType)
    {
        // Act
        if (processType != ProcessType.Unspecified)
        {
            CalculationResultCompletedEventMapperSpy.MapProcessTypeSpy(processType);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() =>
                CalculationResultCompletedEventMapperSpy.MapProcessTypeSpy(processType));
        }
    }

    [Theory]
    [MemberData(nameof(Resolutions))]
    public void Ensure_handling_all_resolutions(Resolution resolution)
    {
        // Act
        if (resolution != Resolution.Unspecified)
        {
            CalculationResultCompletedEventMapperSpy.MapResolutionSpy(resolution);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() =>
                CalculationResultCompletedEventMapperSpy.MapResolutionSpy(resolution));
        }
    }

    [Theory]
    [MemberData(nameof(QuantityQualities))]
    public void Ensure_handling_all_quantity_qualities(QuantityQuality quantityQuality)
    {
        // Act
        if (quantityQuality != QuantityQuality.Unspecified)
        {
            CalculationResultCompletedEventMapperSpy.MapQuantityQualitySpy(quantityQuality);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() =>
                CalculationResultCompletedEventMapperSpy.MapQuantityQualitySpy(quantityQuality));
        }
    }

    [Theory]
    [MemberData(nameof(QuantityUnits))]
    public void Ensure_handling_all_quantity_units(QuantityUnit quantityUnit)
    {
        // Act
        if (quantityUnit != QuantityUnit.Unspecified)
        {
            CalculationResultCompletedEventMapperSpy.MapQuantityUnitSpy(quantityUnit);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() =>
                CalculationResultCompletedEventMapperSpy.MapQuantityUnitSpy(quantityUnit));
        }
    }

    [Theory]
    [MemberData(nameof(TimeSeriesTypes))]
    public void Ensure_handling_all_timeSeries_types(TimeSeriesType timeSeriesType)
    {
        // Act
        if (IsUnsupportedTimeSeriesType(timeSeriesType))
        {
            Assert.Throws<InvalidOperationException>(() =>
                CalculationResultCompletedEventMapperSpy.MapTimeSeriesTypeSpy(timeSeriesType));
        }
        else
        {
            CalculationResultCompletedEventMapperSpy.MapTimeSeriesTypeSpy(timeSeriesType);
        }
    }

    private static bool IsUnsupportedTimeSeriesType(TimeSeriesType timeSeriesType)
    {
        return timeSeriesType is TimeSeriesType.GridLoss
            or TimeSeriesType.TempProduction
            or TimeSeriesType.NegativeGridLoss
            or TimeSeriesType.PositiveGridLoss
            or TimeSeriesType.TempFlexConsumption
            or TimeSeriesType.Unspecified;
    }
}
