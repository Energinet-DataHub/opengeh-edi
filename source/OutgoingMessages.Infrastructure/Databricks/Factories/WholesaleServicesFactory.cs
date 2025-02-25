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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using ChargeTypeMapper = Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults.ChargeTypeMapper;
using Currency = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Currency;
using MeteringPointTypeMapper = Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults.MeteringPointTypeMapper;
using ResolutionMapper = Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults.ResolutionMapper;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;

public static class WholesaleServicesFactory
{
    public static WholesaleServices Create(
        DatabricksSqlRow databricksSqlRow,
        AmountType amountType,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        return amountType switch
        {
            AmountType.AmountPerCharge => GetWholesaleServicesForAmountsPerCharge(databricksSqlRow, timeSeriesPoints),
            AmountType.MonthlyAmountPerCharge => GetWholesaleServicesForMonthlyAmountsPerCharge(databricksSqlRow, timeSeriesPoints),
            AmountType.TotalMonthlyAmount => GetWholesaleServicesForTotalMonthlyAmount(databricksSqlRow, timeSeriesPoints),
            _ => throw new ArgumentOutOfRangeException(nameof(amountType), amountType, null),
        };
    }

    private static WholesaleServices GetWholesaleServicesForAmountsPerCharge(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        var resolution = ResolutionMapper.FromDeltaTableValue(databricksSqlRow[WholesaleResultColumnNames.Resolution]!);
        var period = PeriodHelper.GetPeriod(timeSeriesPoints, resolution);
        var (businessReason, settlementVersion) = BusinessReasonAndSettlementVersionMapper.FromDeltaTableValue(
            databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.CalculationType));

        return new WholesaleServices(
            period,
            databricksSqlRow[AmountsPerChargeViewColumnNames.GridAreaCode]!,
            databricksSqlRow[AmountsPerChargeViewColumnNames.EnergySupplierId]!,
            databricksSqlRow[AmountsPerChargeViewColumnNames.ChargeCode]!,
            ChargeTypeMapper.FromDeltaTableValue(databricksSqlRow[AmountsPerChargeViewColumnNames.ChargeType]!),
            databricksSqlRow[AmountsPerChargeViewColumnNames.ChargeOwnerId]!,
            AmountType.AmountPerCharge,
            resolution,
            QuantityUnitMapper.FromDeltaTableValue(databricksSqlRow[AmountsPerChargeViewColumnNames.QuantityUnit]!),
            MeteringPointTypeMapper.FromDeltaTableValue(databricksSqlRow[AmountsPerChargeViewColumnNames.MeteringPointType]),
            SettlementMethodMapper.FromDeltaTableValue(databricksSqlRow[AmountsPerChargeViewColumnNames.SettlementMethod]),
            Currency.DKK,
            businessReason,
            settlementVersion,
            timeSeriesPoints,
            SqlResultValueConverters.ToInt(databricksSqlRow[AmountsPerChargeViewColumnNames.CalculationVersion]!)!.Value);
    }

    private static WholesaleServices GetWholesaleServicesForMonthlyAmountsPerCharge(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        var period = PeriodHelper.GetPeriod(timeSeriesPoints, Resolution.Monthly);
        var (businessReason, settlementVersion) = BusinessReasonAndSettlementVersionMapper.FromDeltaTableValue(
            databricksSqlRow.ToNonEmptyString(EnergyResultColumnNames.CalculationType));

        return new WholesaleServices(
            period,
            databricksSqlRow[MonthlyAmountsPerChargeViewColumnNames.GridAreaCode]!,
            databricksSqlRow[MonthlyAmountsPerChargeViewColumnNames.EnergySupplierId]!,
            databricksSqlRow[MonthlyAmountsPerChargeViewColumnNames.ChargeCode]!,
            ChargeTypeMapper.FromDeltaTableValue(databricksSqlRow[MonthlyAmountsPerChargeViewColumnNames.ChargeType]!),
            databricksSqlRow[MonthlyAmountsPerChargeViewColumnNames.ChargeOwnerId]!,
            AmountType.MonthlyAmountPerCharge,
            Resolution.Monthly,
            QuantityUnitMapper.FromDeltaTableValue(databricksSqlRow[MonthlyAmountsPerChargeViewColumnNames.QuantityUnit]!),
            null,
            null,
            Currency.DKK,
            businessReason,
            settlementVersion,
            timeSeriesPoints,
            SqlResultValueConverters.ToInt(databricksSqlRow[MonthlyAmountsPerChargeViewColumnNames.CalculationVersion]!)!.Value);
    }

    private static WholesaleServices GetWholesaleServicesForTotalMonthlyAmount(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        var period = PeriodHelper.GetPeriod(timeSeriesPoints, Resolution.Monthly);
        var (businessReason, settlementVersion) = BusinessReasonAndSettlementVersionMapper.FromDeltaTableValue(
            databricksSqlRow.ToNonEmptyString(EnergyResultColumnNames.CalculationType));

        return new WholesaleServices(
            period,
            databricksSqlRow[TotalMonthlyAmountsViewColumnNames.GridAreaCode]!,
            databricksSqlRow[TotalMonthlyAmountsViewColumnNames.EnergySupplierId]!,
            null,
            null,
            databricksSqlRow[TotalMonthlyAmountsViewColumnNames.ChargeOwnerId],
            AmountType.TotalMonthlyAmount,
            Resolution.Monthly,
            null,
            null,
            null,
            Currency.DKK,
            businessReason,
            settlementVersion,
            timeSeriesPoints,
            SqlResultValueConverters.ToInt(databricksSqlRow[TotalMonthlyAmountsViewColumnNames.CalculationVersion]!)!.Value);
    }
}
