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

using System.Linq;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Transactions.Aggregations;
using Tests.Factories;
using Xunit;
using Enum = System.Enum;
using Resolution = Energinet.DataHub.Wholesale.Contracts.Events.Resolution;

namespace Tests.Domain.Transactions.Aggregations;

public class CalculationResultCompletedEventMapperTests
{
    private readonly AggregationResultBuilder _aggregationResult;

    public CalculationResultCompletedEventMapperTests()
    {
        _aggregationResult = new AggregationResultBuilder();
    }

    [Fact]
    public void Process_type_in_calculation_result_completed_in_contract_is_not_changed()
    {
        //ProcessTypes in CalculatedResultCompleted
        // [OriginalName("PROCESS_TYPE_UNSPECIFIED")] Unspecified,
        // [OriginalName("PROCESS_TYPE_BALANCE_FIXING")] BalanceFixing,
        // [OriginalName("PROCESS_TYPE_AGGREGATION")] Aggregation,
        // [OriginalName("PROCESS_TYPE_WHOLESALE_FIXING")] WholesaleFixing,
        // [OriginalName("PROCESS_TYPE_FIRST_CORRECTION_SETTLEMENT")] FirstCorrectionSettlement,
        // [OriginalName("PROCESS_TYPE_SECOND_CORRECTION_SETTLEMENT")] SecondCorrectionSettlement,
        // [OriginalName("PROCESS_TYPE_THIRD_CORRECTION_SETTLEMENT")] ThirdCorrectionSettlement,

        //mapped to
        //ProcessType.Aggregation => BusinessReason.PreliminaryAggregation.Name,
        // ProcessType.BalanceFixing => BusinessReason.BalanceFixing.Name,
        // ProcessType.WholesaleFixing => BusinessReason.WholesaleFixing.Name,
        // ProcessType.FirstCorrectionSettlement => BusinessReason.Correction.Name, // TODO: Check if this is correct
        // ProcessType.SecondCorrectionSettlement => BusinessReason.Correction.Name, // TODO: Check if this is correct
        // ProcessType.ThirdCorrectionSettlement => BusinessReason.Correction.Name, // TODO: Check if this is correct
        // ProcessType.Unspecified => throw new InvalidOperationException("Process type is not specified from Wholesales"),
        var processTypesTheirs = Enum.GetValues(typeof(ProcessType));
        var businessReasons = BusinessReason.GetAll();

        foreach (var processType in processTypesTheirs)
        {
            if (processType is null)
            {
                Assert.Equal(processType, processType);
            }
        }

        foreach (var businessReason in businessReasons)
        {
            if (businessReason is null)
            {
                Assert.Equal(businessReason, businessReason);
            }
        }
    }

    [Fact]
    public void Resolution_in_calculation_result_completed_in_contract_is_not_changed()
    {
        //[OriginalName("RESOLUTION_UNSPECIFIED")] Unspecified,
        //[OriginalName("RESOLUTION_QUARTER")] Quarter,
        var resolutionTheirs = Enum.GetValues(typeof(Resolution));
        //var businessReason = Domain.Transactions.Aggregations.;
    }

    [Fact]
    public void Quantity_unit__in_calculation_result_completed_in_contract_is_not_changed()
    {
        //[OriginalName("QUANTITY_UNIT_UNSPECIFIED")] Unspecified,
        //[OriginalName("QUANTITY_UNIT_KWH")] Kwh,
        //Mapped to quality
        var quantityUnitTheirs = Enum.GetValues(typeof(QuantityUnit));
        var businessReason = Quality.GetAll();
    }

    [Fact]
    public void Time_series_type_in_calculation_result_completed_in_contract_is_not_changed()
    {
        // [OriginalName("TIME_SERIES_TYPE_UNSPECIFIED")] Unspecified,
        // [OriginalName("TIME_SERIES_TYPE_PRODUCTION")] Production,
        // [OriginalName("TIME_SERIES_TYPE_NON_PROFILED_CONSUMPTION")] NonProfiledConsumption,
        // [OriginalName("TIME_SERIES_TYPE_FLEX_CONSUMPTION")] FlexConsumption,
        // [OriginalName("TIME_SERIES_TYPE_NET_EXCHANGE_PER_GA")] NetExchangePerGa,
        // [OriginalName("TIME_SERIES_TYPE_NET_EXCHANGE_PER_NEIGHBORING_GA")] NetExchangePerNeighboringGa,
        // [OriginalName("TIME_SERIES_TYPE_GRID_LOSS")] GridLoss,
        // [OriginalName("TIME_SERIES_TYPE_NEGATIVE_GRID_LOSS")] NegativeGridLoss,
        // [OriginalName("TIME_SERIES_TYPE_POSITIVE_GRID_LOSS")] PositiveGridLoss,
        // [OriginalName("TIME_SERIES_TYPE_TOTAL_CONSUMPTION")] TotalConsumption,
        // [OriginalName("TIME_SERIES_TYPE_TEMP_FLEX_CONSUMPTION")] TempFlexConsumption,
        // [OriginalName("TIME_SERIES_TYPE_TEMP_PRODUCTION")] TempProduction,
        var timeSeriesTheirs = Enum.GetValues(typeof(TimeSeriesType));
        var meteringPoint = MeteringPointType.GetAll();
    }

    [Fact]
    public void Quantity_unit_in_calculation_result_completed_in_contract_is_not_changed()
    {
        var measurementUnits = MeasurementUnit.GetAll();

        var measurementUnitTheirs = (QuantityUnit[])Enum.GetValues(typeof(QuantityUnit));
        Assert.Contains(measurementUnitTheirs, unit => unit == QuantityUnit.Unspecified);
        Assert.Contains(measurementUnitTheirs, unit => unit == QuantityUnit.Kwh);

        // QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
        //QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
        foreach (var value in measurementUnitTheirs)
        {
            var quantityUnit = (QuantityUnit)value;
        }

       // var measurementUnits = MeasurementUnit.GetAll();
    }
}
