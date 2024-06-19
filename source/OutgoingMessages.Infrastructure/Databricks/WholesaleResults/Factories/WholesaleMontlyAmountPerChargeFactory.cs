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

public class WholesaleMontlyAmountPerChargeFactory
{
    public static WholesaleMonthlyAmountPerCharge Create(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        var period = GetPeriod(timeSeriesPoints);

        return new WholesaleMonthlyAmountPerCharge(
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
            ChargeTypeMapper.FromDeltaTableValue(
                databricksSqlRow.ToNonEmptyString(WholesaleResultColumnNames.ChargeType)),
            Resolution.Monthly,
            timeSeriesPoints,
            databricksSqlRow.ToBool(WholesaleResultColumnNames.IsTax),
            databricksSqlRow.ToNullableString(WholesaleResultColumnNames.ChargeCode));
    }

    private static (Instant Start, Instant End) GetPeriod(
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        var start = timeSeriesPoints.Min(x => x.TimeUtc);
        var end = timeSeriesPoints.Max(x => x.TimeUtc);
        // The end date is the start of the next period.
        var endWithResolutionOffset = end.ToDateTimeOffset().AddMonths(1);
        return (start, endWithResolutionOffset.ToInstant());
    }
}
