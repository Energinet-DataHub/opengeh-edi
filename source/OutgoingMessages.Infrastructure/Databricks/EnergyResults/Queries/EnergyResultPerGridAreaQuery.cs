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

using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;

/// <summary>
/// The query we must perform on the 'Energy Result Per Grid Area' view.
///
/// Keep the code updated with regards to the documentation in confluence in a way
/// that it is easy to compare (e.g. order of columns).
/// See confluence: https://energinet.atlassian.net/wiki/spaces/D3/pages/849805314/Calculation+Result+Model#Energy-Result-Points-Per-Grid-Area
/// </summary>
public class EnergyResultPerGridAreaQuery(
        EdiDatabricksOptions ediDatabricksOptions,
        Guid calculationId)
    : EnergyResultQueryBase<EnergyResultPerGridArea>(
        ediDatabricksOptions,
        calculationId)
{
    public override string DataObjectName => "energy_result_points_per_ga_v1";

    public override Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition => new()
    {
        { EnergyResultColumnNames.CalculationId,                (DeltaTableCommonTypes.String,          false) },
        { EnergyResultColumnNames.CalculationType,              (DeltaTableCommonTypes.String,          false) },
        { EnergyResultColumnNames.CalculationPeriodStart,       (DeltaTableCommonTypes.Timestamp,       false) },
        { EnergyResultColumnNames.CalculationPeriodEnd,         (DeltaTableCommonTypes.Timestamp,       false) },
        { EnergyResultColumnNames.CalculationVersion,           (DeltaTableCommonTypes.BigInt,          false) },
        { EnergyResultColumnNames.ResultId,                     (DeltaTableCommonTypes.String,          false) },
        { EnergyResultColumnNames.GridAreaCode,                 (DeltaTableCommonTypes.String,          false) },
        { EnergyResultColumnNames.MeteringPointType,            (DeltaTableCommonTypes.String,          false) },
        { EnergyResultColumnNames.SettlementMethod,             (DeltaTableCommonTypes.String,          true) },
        { EnergyResultColumnNames.Resolution,                   (DeltaTableCommonTypes.String,          false) },
        { EnergyResultColumnNames.Time,                         (DeltaTableCommonTypes.Timestamp,       false) },
        { EnergyResultColumnNames.Quantity,                     (DeltaTableCommonTypes.Decimal18x3,     false) },
        { EnergyResultColumnNames.QuantityUnit,                 (DeltaTableCommonTypes.String,          false) },
        { EnergyResultColumnNames.QuantityQualities,            (DeltaTableCommonTypes.ArrayOfStrings,  false) },
    };

    protected override EnergyResultPerGridArea CreateEnergyResult(DatabricksSqlRow databricksSqlRow, IReadOnlyCollection<EnergyTimeSeriesPoint> timeSeriesPoints)
    {
        return EnergyResultPerGridAreaFactory.CreateEnergyResultPerGridArea(databricksSqlRow, timeSeriesPoints);
    }
}