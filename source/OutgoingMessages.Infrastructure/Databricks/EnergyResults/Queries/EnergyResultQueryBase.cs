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

using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;

public abstract class EnergyResultQueryBase(
        IOptions<EdiDatabricksOptions> ediDatabricksOptions,
        Guid calculationId)
    : DatabricksStatement, IDeltaTableSchemaDescription
{
    private readonly EdiDatabricksOptions _ediDatabricksOptions = ediDatabricksOptions.Value;

    /// <summary>
    /// Name of database to query in.
    /// </summary>
    public string DatabaseName => _ediDatabricksOptions.DatabaseName;

    /// <summary>
    /// Name of view or table to query in.
    /// </summary>
    public abstract string DataObjectName { get; }

    public Guid CalculationId { get; } = calculationId;

    /// <summary>
    /// List of column names to select in query.
    /// </summary>
    public string[] SqlColumnNames => SchemaDefinition.Keys.ToArray();

    /// <summary>
    /// The schema definition of the view expressed as (Column name, Data type, Is nullable).
    ///
    /// Can be used in tests to create a matching data object (e.g. table).
    /// </summary>
    public abstract Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition { get; }

    protected override string GetSqlStatement()
    {
        return $@"
SELECT {string.Join(", ", SqlColumnNames)}
FROM {DatabaseName}.{DataObjectName}
WHERE {EnergyResultColumnNames.CalculationId} = '{CalculationId}'
ORDER BY {EnergyResultColumnNames.ResultId}, {EnergyResultColumnNames.Time}
";
    }
}