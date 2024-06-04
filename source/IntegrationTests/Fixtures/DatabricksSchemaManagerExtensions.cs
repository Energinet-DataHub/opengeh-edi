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

using System.Globalization;
using CsvHelper;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;

namespace Energinet.DataHub.EDI.IntegrationTests.Fixtures;

// TODO: Can we extract our extensions to TestCommon in a way that it is useful for other teams?
public static class DatabricksSchemaManagerExtensions
{
    /// <summary>
    /// Create table based on given schema information.
    /// </summary>
    public static Task CreateTableAsync(this DatabricksSchemaManager schemaManager, IDeltaTableSchemaDescription schemaInfomation)
    {
        return schemaManager.CreateTableAsync(schemaInfomation.DataObjectName, schemaInfomation.SchemaDefinition);
    }

    /// <summary>
    /// Expects a CSV file which was exported from Databricks.
    /// It means the header must contain the column names and each row must contain the delta table values per column.
    /// Delta table arrays must be parsed differently, but otherwise all values can be parsed from the CSV into strings.
    /// All parsed rows are then inserted into a delta table.
    /// </summary>
    public static async Task InsertFromCsvFileAsync(this DatabricksSchemaManager schemaManager, IDeltaTableSchemaDescription schemaInfomation, string testFilePath)
    {
        using (var streamReader = new StreamReader(testFilePath))
        using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
        {
            csvReader.Read();
            csvReader.ReadHeader();

            var rows = new List<string[]>();
            while (csvReader.Read())
            {
                var row = new string[csvReader.HeaderRecord.Length];
                for (var columnIndex = 0; columnIndex < csvReader.ColumnCount; columnIndex++)
                {
                    row[columnIndex] = ParseColumnValue(schemaInfomation, csvReader, columnIndex);
                }

                rows.Add(row);
            }

            await schemaManager.InsertAsync(schemaInfomation.DataObjectName, csvReader.HeaderRecord, rows);
        }
    }

    /// <summary>
    /// Parse CSV column value into a delta table "insertable" value.
    /// Only arrays require special handling; all other values can be inserted as "strings".
    /// </summary>
    private static string ParseColumnValue(IDeltaTableSchemaDescription schemaInfomation, CsvReader csvReader, int columnIndex)
    {
        var columnName = csvReader.HeaderRecord[columnIndex];
        var columnValue = csvReader.GetField(columnIndex);

        if (schemaInfomation.SchemaDefinition[columnName].DataType == DeltaTableCommonTypes.ArrayOfStrings)
        {
            var arrayContent = columnValue
                .Replace('[', '(')
                .Replace(']', ')');

            return $"Array{arrayContent}";
        }

        if (schemaInfomation.SchemaDefinition[columnName].IsNullable && columnValue == string.Empty)
        {
            return "NULL";
        }

        return $"'{columnValue}'";
    }
}
