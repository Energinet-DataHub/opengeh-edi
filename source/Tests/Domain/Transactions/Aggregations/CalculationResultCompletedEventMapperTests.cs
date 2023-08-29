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

using Energinet.DataHub.Wholesale.Contracts.Events;
using Xunit;
using Enum = System.Enum;
using Resolution = Energinet.DataHub.Wholesale.Contracts.Events.Resolution;

namespace Tests.Domain.Transactions.Aggregations;

public class CalculationResultCompletedEventMapperTests
{
    public CalculationResultCompletedEventMapperTests()
    {
    }

    [Fact]
    public void Calculation_result_completed_processType_from_wholesale_is_not_changed()
    {
        //mapped to
        //ProcessType.Aggregation => BusinessReason.PreliminaryAggregation.Name,
        // ProcessType.BalanceFixing => BusinessReason.BalanceFixing.Name,
        // ProcessType.WholesaleFixing => BusinessReason.WholesaleFixing.Name,
        // ProcessType.FirstCorrectionSettlement => BusinessReason.Correction.Name, // TODO: Check if this is correct
        // ProcessType.SecondCorrectionSettlement => BusinessReason.Correction.Name, // TODO: Check if this is correct
        // ProcessType.ThirdCorrectionSettlement => BusinessReason.Correction.Name, // TODO: Check if this is correct
        // ProcessType.Unspecified => throw new InvalidOperationException("Process type is not specified from Wholesales"),
        var processTypeTheirs = (ProcessType[])Enum.GetValues(typeof(ProcessType));
        Assert.Contains(processTypeTheirs, type => type == ProcessType.Unspecified);
        Assert.Contains(processTypeTheirs, type => type == ProcessType.Aggregation);
        Assert.Contains(processTypeTheirs, type => type == ProcessType.BalanceFixing);
        Assert.Contains(processTypeTheirs, type => type == ProcessType.SecondCorrectionSettlement);
        Assert.Contains(processTypeTheirs, type => type == ProcessType.WholesaleFixing);
        Assert.Contains(processTypeTheirs, type => type == ProcessType.FirstCorrectionSettlement);
        Assert.Contains(processTypeTheirs, type => type == ProcessType.ThirdCorrectionSettlement);
    }

    [Fact]
    public void Calculation_result_completed_resolution_from_wholesale_is_not_changed()
    {
        var resolutionTheirs = (Resolution[])Enum.GetValues(typeof(Resolution));
        Assert.Contains(resolutionTheirs, resolution => resolution == Resolution.Quarter);
        Assert.Contains(resolutionTheirs, resolution => resolution == Resolution.Unspecified);
    }

    [Fact]
    public void Calculation_result_completed_quantityQuality_from_wholesale_is_not_changed()
    {
        var quantityQualityTheirs = (QuantityQuality[])Enum.GetValues(typeof(QuantityQuality));
        Assert.Contains(quantityQualityTheirs, quality => quality == QuantityQuality.Unspecified);
        Assert.Contains(quantityQualityTheirs, quality => quality == QuantityQuality.Calculated);
        Assert.Contains(quantityQualityTheirs, quality => quality == QuantityQuality.Estimated);
        Assert.Contains(quantityQualityTheirs, quality => quality == QuantityQuality.Incomplete);
        Assert.Contains(quantityQualityTheirs, quality => quality == QuantityQuality.Measured);
        Assert.Contains(quantityQualityTheirs, quality => quality == QuantityQuality.Missing);

        //var businessReason = Quality.GetAll();
    }

    [Fact]
    public void Calculation_result_completed_timeSeriesType_from_wholesale_is_not_changed()
    {
        var timeSeriesTypeTheirs = (TimeSeriesType[])Enum.GetValues(typeof(TimeSeriesType));
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.Production);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.Production);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.NonProfiledConsumption);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.FlexConsumption);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.NetExchangePerGa);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.NetExchangePerNeighboringGa);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.GridLoss);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.NegativeGridLoss);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.PositiveGridLoss);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.TotalConsumption);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.TempFlexConsumption);
        Assert.Contains(timeSeriesTypeTheirs, type => type == TimeSeriesType.TempProduction);
    }

    [Fact]
    public void Calculation_result_completed_quantity_from_wholesale_is_not_changed()
    {
        //var measurementUnits = MeasurementUnit.GetAll();
        var measurementUnitTheirs = (QuantityUnit[])Enum.GetValues(typeof(QuantityUnit));
        Assert.Contains(measurementUnitTheirs, unit => unit == QuantityUnit.Unspecified);
        Assert.Contains(measurementUnitTheirs, unit => unit == QuantityUnit.Kwh);
    }
}
