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

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;

/// <summary>
/// Contains information about a Databricks delta table schema.
/// </summary>
public interface IDeltaTableSchemaDescription
{
    /// <summary>
    /// Name of database.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// Name of view or table.
    /// </summary>
    public string DataObjectName { get; }

    /// <summary>
    /// The schema definition of the view or table expressed as (Column name, Data type, Is nullable).
    ///
    /// Can be used in tests to create a matching data object (e.g. table).
    /// </summary>
    public Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition { get; }
}
