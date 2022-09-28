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

using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;

namespace IntegrationService;

public static class SyncMarketEvaluationPoints
{
    [Function("SyncMarketEvaluationPoints")]
    public static Task RunAsync([TimerTrigger("*/30 * * * *")] TimerInfo timerTimerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("SyncMarketEvaluationPoints");
        return FetchMarketEvaluationPointsAsync();
    }

    private static async Task FetchMarketEvaluationPointsAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("METERING_POINT_DB_CONNECTION_STRING");
        using var meteringPointsconnection = new SqlConnection(connectionString);
        await meteringPointsconnection.OpenAsync().ConfigureAwait(false);
        var selectStatement = @"SELECT mp.Id, mp.GsrnNumber, g.ActorId as GridOperatorId " +
                                "FROM [dbo].[MeteringPoints] mp " +
                                "JOIN [dbo].[GridAreaLinks] gl ON gl.Id = mp.MeteringGridArea " +
                                "JOIN [dbo].[GridAreas] g ON g.Id = gl.GridAreaId";
        var command = meteringPointsconnection.CreateCommand();
        command.CommandText = selectStatement;
        var result = await command.ExecuteReaderAsync().ConfigureAwait(false);

        using var messagingConnection =
            new SqlConnection(Environment.GetEnvironmentVariable("MESSAGING_DB_CONNECTION_STRING"));
        await messagingConnection.OpenAsync().ConfigureAwait(false);
        var transaction = await messagingConnection.BeginTransactionAsync().ConfigureAwait(false);
        while (await result.ReadAsync().ConfigureAwait(false))
        {
            var id = result.GetGuid(0);
            var meteringPointNumber = result.GetString(1);
            var gridOperatorId = result.GetGuid(2);

            var insertStatement = @"
                                    BEGIN
                                        IF NOT EXISTS (SELECT * FROM b2b.MarketEvaluationPoints WHERE MarketEvaluationPointNumber = @MarketEvaluationPointNumber)
                                            BEGIN
                                                INSERT INTO b2b.MarketEvaluationPoints (Id, MarketEvaluationPointNumber, GridOperatorId, EnergySupplierNumber)
                                                VALUES (@Id, @MarketEvaluationPointNumber, @GridOperatorId, '')
                                            END
                                        ELSE
                                            BEGIN
                                                UPDATE b2b.MarketEvaluationPoints
                                                SET GridOperatorId = @GridOperatorId, Id = @Id
                                                WHERE MarketEvaluationPointNumber = @MarketEvaluationPointNumber
                                            END
                                    END";
            var insertCommand = messagingConnection.CreateCommand();
            insertCommand.Transaction = transaction as SqlTransaction;
            insertCommand.CommandText = insertStatement;
            insertCommand.Parameters.AddWithValue("@Id", id);
            insertCommand.Parameters.AddWithValue("@MarketEvaluationPointNumber", meteringPointNumber);
            insertCommand.Parameters.AddWithValue("@GridOperatorId", gridOperatorId);
            await insertCommand.ExecuteScalarAsync().ConfigureAwait(false);
        }

        await transaction.CommitAsync().ConfigureAwait(false);
    }
}
