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
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;

public class InternalCommandsRetention : IDataRetention
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ILogger<InternalCommandsRetention> _logger;
    private readonly IAuditLogger _auditLogger;
    private readonly IClock _clock;

    public InternalCommandsRetention(
        IDatabaseConnectionFactory databaseConnectionFactory,
        ILogger<InternalCommandsRetention> logger,
        IAuditLogger auditLogger,
        IClock clock)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _logger = logger;
        _auditLogger = auditLogger;
        _clock = clock;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var anyOldInternaCommands = await GetAmountOfRemainingCommandsAsync(cancellationToken).ConfigureAwait(false);
        while (anyOldInternaCommands)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested. Exiting cleanup loop.");
                cancellationToken.ThrowIfCancellationRequested();
            }

            await LogAuditAsync().ConfigureAwait(false);
            await DeleteOldCommandsAsync(cancellationToken).ConfigureAwait(false);
            anyOldInternaCommands = await GetAmountOfRemainingCommandsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DeleteOldCommandsAsync(CancellationToken cancellationToken)
    {
        const string deleteStmt = @"
            WITH CTE AS
             (
                 SELECT TOP 500 *
                 FROM [dbo].[QueuedInternalCommands]
                  WHERE [ProcessedDate] IS NOT NULL AND [ErrorMessage] IS NULL
             )
            DELETE FROM CTE;";

        using var connection = (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = deleteStmt;

        try
        {
            var numberDeletedRecords = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully deleted {NumberDeletedQueuedInternalCommand} of old commands", numberDeletedRecords);
        }
        catch (DbException e)
        {
            _logger.LogError(e, "Failed to delete old commands: {ErrorMessage}", e.Message);
            throw;
        }
    }

    private async Task LogAuditAsync()
    {
        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.RetentionDeletion,
                activityOrigin: nameof(ADayHasPassed),
                activityPayload: _clock.GetCurrentInstant(),
                affectedEntityType: AuditLogEntityType.InternalCommand,
                affectedEntityKey: null)
            .ConfigureAwait(false);
    }

    private async Task<bool> GetAmountOfRemainingCommandsAsync(CancellationToken cancellationToken)
    {
        const string selectStmt = @"SELECT CASE WHEN EXISTS (
            SELECT 1 FROM [dbo].[QueuedInternalCommands]
            WHERE [ProcessedDate] IS NOT NULL AND [ErrorMessage] IS NULL
        ) THEN 1 ELSE 0 END";

        using var connection = (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = selectStmt;

        try
        {
            var exists = (int)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Existence of old commands: {Exists}", exists == 1);
            return exists == 1;
        }
        catch (DbException e)
        {
            _logger.LogError(e, "Failed to get number of old integration events: {ErrorMessage}", e.Message);
            throw;
        }
    }
}
