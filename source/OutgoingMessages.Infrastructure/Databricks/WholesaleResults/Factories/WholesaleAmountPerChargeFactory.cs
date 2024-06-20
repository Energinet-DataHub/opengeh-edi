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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Factories;

public class WholesaleAmountPerChargeFactory
{
    public static WholesaleAmountPerCharge CreatewholesaleResultForAmountPerCharge(DatabricksSqlRow databricksSqlRow, IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        var resolution = ResolutionMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.Resolution));
        var period = GetPeriod(timeSeriesPoints, resolution);

        return new WholesaleAmountPerCharge(
            databricksSqlRow.ToGuid(WholesaleResultColumnNames.ResultId),
            databricksSqlRow.ToGuid(WholesaleResultColumnNames.CalculationId),
            CalculationTypeMapper.FromDeltaTableValue(
                databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.CalculationType)),
            databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.GridAreaCode),
            databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.EnergySupplierId),
            databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.ChargeOwnerId),
            databricksSqlRow.ToLong(WholesaleResultColumnNames.CalculationVersion),
            period.Start,
            period.End,
            MeasurementUnitMapper.Map(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.QuantityUnit)),
            CurrencyMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.Currency)),
            ChargeTypeMapper.FromDeltaTableValue(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.ChargeType)),
            resolution,
            MeteringPointTypeMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.MeteringPointType)),
            SettlementMethodMapper.FromDeltaTableValue(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.SettlementMethod)),
            timeSeriesPoints,
            databricksSqlRow.ToBool(WholesaleResultColumnNames.IsTax),
            databricksSqlRow.ToNullableString(WholesaleResultColumnNames.ChargeCode));
    }

    private static (Instant Start, Instant End) GetPeriod(IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints, Resolution resolution)
    {
        var start = timeSeriesPoints.Min(x => x.TimeUtc);
        var end = timeSeriesPoints.Max(x => x.TimeUtc);
        // The end date is the start of the next period.
        var endWithResolutionOffset = GetDateTimeWithResolutionOffset(resolution, end.ToDateTimeOffset());
        return (start, endWithResolutionOffset.ToInstant());
    }

    private static DateTimeOffset GetDateTimeWithResolutionOffset(Resolution resolution, DateTimeOffset dateTime)
    {
        switch (resolution)
        {
            case var res when res == Resolution.Hourly:
                return dateTime.AddMinutes(60);
            case var res when res == Resolution.Daily:
                return dateTime.AddDays(1);
            case var res when res == Resolution.Monthly:
                return dateTime.AddMonths(1);
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(resolution),
                    resolution,
                    "Unknown databricks resolution");
        }
    }
}
