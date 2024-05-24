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

using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution.Formats;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

// TODO: Decide where code for accessing DataLake should be placed; for now I'm just writing it as plain code within the activity.
// TODO: Decide if we need to reference NuGet package "Energinet.DataHub.Core.Databricks.SqlStatementExecution" directly here, or not.
internal class EnqueueMessagesActivity(
    DatabricksSqlWarehouseQueryExecutor warehouseQueryExecutor)
{
    private readonly DatabricksSqlWarehouseQueryExecutor _warehouseQueryExecutor = warehouseQueryExecutor;

    [Function(nameof(EnqueueMessagesActivity))]
    public async Task Run(
        [ActivityTrigger] EnqueueMessagesInput input)
    {
        // TODO: Decide "view" based on calculation type
        var modelName = "wholesale_edi_results";
        var tableName = "energy_result_points_per_ga_v1";

        // TODO:
        // Instead of a raw sql statement, we can encapsulate queries by inheriting from "DatabricksStatement".
        // See example "EnergyResultQueryStatement" in Wholesale
        var statement = DatabricksStatement
            .FromRawSql($"SELECT * FROM {modelName}.{tableName} WHERE calculation_id = '{input.CalculationId}'")
            .Build();

        // TODO: What format is best? Json / Arrow ?
        await foreach (var nextRow in _warehouseQueryExecutor.ExecuteStatementAsync(statement, Format.JsonArray))
        {
            // TODO: Parse/map data from row into "Outgoing message" type and send to processor
        }
    }
}
