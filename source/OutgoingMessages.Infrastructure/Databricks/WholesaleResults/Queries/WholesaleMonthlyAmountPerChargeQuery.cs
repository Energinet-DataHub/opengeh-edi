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

using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

public class WholesaleMonthlyAmountPerChargeQuery(
    EdiDatabricksOptions ediDatabricksOptions,
    Guid calculationId)
    : WholesaleResultQueryBase<WholesaleMontlyAmountPerChargeDto>(
        ediDatabricksOptions,
        calculationId)
{
    public override string DataObjectName => "monthly_amounts_per_charge_v1";

    public override Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition => new()
    {
        { WholesaleResultColumnNames.CalculationId,             (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.CalculationType,           (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.CalculationVersion,        (DeltaTableCommonTypes.BigInt,              false) },
        { WholesaleResultColumnNames.ResultId,                  (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.GridAreaCode,              (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.EnergySupplierId,          (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.ChargeCode,                (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.ChargeType,                (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.ChargeOwnerId,             (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.QuantityUnit,              (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.IsTax,                     (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.Currency,                  (DeltaTableCommonTypes.String,              false) },
        { WholesaleResultColumnNames.Time,                      (DeltaTableCommonTypes.Timestamp,           false) },
        { WholesaleResultColumnNames.Amount,                    (DeltaTableCommonTypes.Decimal18x3,         true) },
    };

    protected override Task<WholesaleMontlyAmountPerChargeDto> CreateWholesaleResultAsync(DatabricksSqlRow databricksSqlRow, IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints)
    {
        throw new NotImplementedException();
    }
}
