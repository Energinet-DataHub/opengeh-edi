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
using System.Reflection;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Infrastructure.Transactions.Aggregations;
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
        var method = GetMethod("MapProcessTypeFromCalculationResult");

        // Act
        if (processType != ProcessType.Unspecified)
        {
            method.Invoke(obj: null, parameters: new object[] { processType });
        }
        else
        {
            AssertExpectedUnhandledMapping(processType, method);
        }
    }

    [Theory]
    [MemberData(nameof(Resolutions))]
    public void Ensure_handling_all_resolutions(Resolution resolution)
    {
        var method = GetMethod("MapResolutionFromCalculationResult");

        // Act
        if (resolution != Resolution.Unspecified)
        {
            method.Invoke(obj: null, parameters: new object[] { resolution });
        }
        else
        {
            AssertExpectedUnhandledMapping(resolution, method);
        }
    }

    [Theory]
    [MemberData(nameof(QuantityQualities))]
    public void Ensure_handling_all_quantity_qualities(QuantityQuality quantityQuality)
    {
        var method = GetMethod("MapQualityFromCalculationResult");

        // Act
        if (quantityQuality != QuantityQuality.Unspecified)
        {
            method.Invoke(obj: null, parameters: new object[] { quantityQuality });
        }
        else
        {
            AssertExpectedUnhandledMapping(quantityQuality, method);
        }
    }

    [Theory]
    [MemberData(nameof(QuantityUnits))]
    public void Ensure_handling_all_quantity_units(QuantityUnit quantityUnit)
    {
        var method = GetMethod("MapQuantityUnitFromCalculationResult");

        // Act
        if (quantityUnit != QuantityUnit.Unspecified)
        {
            method.Invoke(obj: null, parameters: new object[] { quantityUnit });
        }
        else
        {
            AssertExpectedUnhandledMapping(quantityUnit, method);
        }
    }

    [Theory]
    [MemberData(nameof(TimeSeriesTypes))]
    public void Ensure_handling_all_timeSeries_types(TimeSeriesType timeSeriesType)
    {
        var method = GetMethod("MapMeteringPointTypeFromCalculationResult");

        // Act
        if (IsUnsupportedTimeSeriesType(timeSeriesType))
        {
            AssertExpectedUnhandledMapping(timeSeriesType, method);
        }
        else
        {
            method.Invoke(obj: null, parameters: new object[] { timeSeriesType });
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

    private static MethodInfo GetMethod(string name)
    {
        var method = typeof(AggregationFactory).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return method;
    }

    private static void AssertExpectedUnhandledMapping(object processType, MethodInfo method)
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            try
            {
                return method.Invoke(obj: null, parameters: new[] { processType });
            }
            catch (Exception e)
            {
                throw e.InnerException!;
            }
        });
    }
}
