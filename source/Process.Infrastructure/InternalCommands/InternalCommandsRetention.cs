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

using System.Data.Common;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;

public class InternalCommandsRetention : IDataRetention
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ILogger<InternalCommandsRetention> _logger;

    public InternalCommandsRetention(
        IDatabaseConnectionFactory databaseConnectionFactory,
        ILogger<InternalCommandsRetention> logger)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _logger = logger;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var amountOfOldEvents = await GetAmountOfRemainingCommandsAsync(cancellationToken).ConfigureAwait(false);
        while (amountOfOldEvents > 0)
        {
            const string deleteStmt = @"
                WITH CTE AS
                 (
                     SELECT TOP 500 *
                     FROM [dbo].[QueuedInternalCommands]
                      WHERE [ProcessedDate] IS NOT NULL AND [ErrorMessage] IS NULL
                 )
                DELETE FROM CTE;";

            using var connection =
                (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction =
                (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = deleteStmt;

            try
            {
                var numberDeletedRecords = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Successfully deleted {NumberDeletedQueuedInternalCommand} of old commands", numberDeletedRecords);
            }
            catch (DbException)
            {
                // Add exception logging
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw; // re-throw exception
            }

            amountOfOldEvents = await GetAmountOfRemainingCommandsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int> GetAmountOfRemainingCommandsAsync(CancellationToken cancellationToken)
    {
        const string selectStmt = @"SELECT Count(*) FROM [dbo].[QueuedInternalCommands]
                    WHERE [ProcessedDate] IS NOT NULL AND [ErrorMessage] IS NULL";

        using var connection =
            (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken)
                .ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = selectStmt;

        try
        {
            var amountOfOldEvents = (int)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Number of old integration events: {AmountOfOldEvents} to be deleted", amountOfOldEvents);
            return amountOfOldEvents;
        }
        catch (DbException e)
        {
            _logger.LogError(e, "Failed to get number of old integration events: {ErrorMessage}", e.Message);
            throw; // re-throw exception
        }
    }
}
