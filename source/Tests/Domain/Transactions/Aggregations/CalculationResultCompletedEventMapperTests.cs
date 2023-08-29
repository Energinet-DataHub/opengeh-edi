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

using Domain.OutgoingMessages;
using Domain.Transactions.Aggregations;
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
        var processTypeTheirs = (ProcessType[])Enum.GetValues(typeof(ProcessType));

        Assert.Collection(
            processTypeTheirs,
            item => Assert.Equal(ProcessType.Unspecified, item),
            item => Assert.Equal(ProcessType.BalanceFixing, item),
            item => Assert.Equal(ProcessType.Aggregation, item),
            item => Assert.Equal(ProcessType.WholesaleFixing, item),
            item => Assert.Equal(ProcessType.FirstCorrectionSettlement, item),
            item => Assert.Equal(ProcessType.SecondCorrectionSettlement, item),
            item => Assert.Equal(ProcessType.ThirdCorrectionSettlement, item));

        Assert.Equal(7, processTypeTheirs.Length);
        var businessReasons = BusinessReason.GetAll();
        //mapped to
        //ProcessType.Aggregation => BusinessReason.PreliminaryAggregation.Name,
        // ProcessType.BalanceFixing => BusinessReason.BalanceFixing.Name,
        // ProcessType.WholesaleFixing => BusinessReason.WholesaleFixing.Name,
        // ProcessType.FirstCorrectionSettlement => BusinessReason.Correction.Name, // TODO: Check if this is correct
        // ProcessType.SecondCorrectionSettlement => BusinessReason.Correction.Name, // TODO: Check if this is correct
        // ProcessType.ThirdCorrectionSettlement => BusinessReason.Correction.Name, // TODO: Check if this is correct
        // ProcessType.Unspecified => throw new InvalidOperationException("Process type is not specified from Wholesales"),
    }

    [Fact]
    public void Calculation_result_completed_resolution_from_wholesale_is_not_changed()
    {
        var resolutionTheirs = (Resolution[])Enum.GetValues(typeof(Resolution));
        Assert.Collection(
            resolutionTheirs,
            item => Assert.Equal(Resolution.Unspecified, item),
            item => Assert.Equal(Resolution.Quarter, item));

        Assert.Equal(2, resolutionTheirs.Length);
        var resolutions = global::Domain.Transactions.Aggregations.Resolution.GetAll();
    }

    [Fact]
    public void Calculation_result_completed_quantityQuality_from_wholesale_is_not_changed()
    {
        var quantityQualityTheirs = (QuantityQuality[])Enum.GetValues(typeof(QuantityQuality));

        Assert.Collection(
            quantityQualityTheirs,
            item => Assert.Equal(QuantityQuality.Unspecified, item),
            item => Assert.Equal(QuantityQuality.Estimated, item),
            item => Assert.Equal(QuantityQuality.Measured, item),
            item => Assert.Equal(QuantityQuality.Missing, item),
            item => Assert.Equal(QuantityQuality.Incomplete, item),
            item => Assert.Equal(QuantityQuality.Calculated, item));

        Assert.Equal(6, quantityQualityTheirs.Length);

        var quantityQuality = Quality.GetAll();
    }

    [Fact]
    public void Calculation_result_completed_timeSeriesType_from_wholesale_is_not_changed()
    {
        var timeSeriesTypeTheirs = (TimeSeriesType[])Enum.GetValues(typeof(TimeSeriesType));
        Assert.Collection(
            timeSeriesTypeTheirs,
            item => Assert.Equal(TimeSeriesType.Unspecified, item),
            item => Assert.Equal(TimeSeriesType.Production, item),
            item => Assert.Equal(TimeSeriesType.NonProfiledConsumption, item),
            item => Assert.Equal(TimeSeriesType.FlexConsumption, item),
            item => Assert.Equal(TimeSeriesType.NetExchangePerGa, item),
            item => Assert.Equal(TimeSeriesType.NetExchangePerNeighboringGa, item),
            item => Assert.Equal(TimeSeriesType.GridLoss, item),
            item => Assert.Equal(TimeSeriesType.NegativeGridLoss, item),
            item => Assert.Equal(TimeSeriesType.PositiveGridLoss, item),
            item => Assert.Equal(TimeSeriesType.TotalConsumption, item),
            item => Assert.Equal(TimeSeriesType.TempFlexConsumption, item),
            item => Assert.Equal(TimeSeriesType.TempProduction, item));
        Assert.Equal(12, timeSeriesTypeTheirs.Length);

        var timeSeriesTypes = MeteringPointType.GetAll();
    }

    [Fact]
    public void Calculation_result_completed_quantity_from_wholesale_is_not_changed()
    {
        var measurementUnitTheirs = (QuantityUnit[])Enum.GetValues(typeof(QuantityUnit));
        Assert.Collection(
            measurementUnitTheirs,
            item => Assert.Equal(QuantityUnit.Unspecified, item),
            item => Assert.Equal(QuantityUnit.Kwh, item));
        Assert.Equal(2, measurementUnitTheirs.Length);

        var measurementUnits = MeasurementUnit.GetAll();
    }
}
