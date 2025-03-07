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

using System.Data.Common;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure;

public class ReceivedIntegrationEventsRetention : IDataRetention
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly IClock _clock;
    private readonly ILogger<ReceivedIntegrationEventsRetention> _logger;
    private readonly IAuditLogger _auditLogger;

    public ReceivedIntegrationEventsRetention(
        IDatabaseConnectionFactory databaseConnectionFactory,
        IClock clock,
        ILogger<ReceivedIntegrationEventsRetention> logger,
        IAuditLogger auditLogger)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _clock = clock;
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var monthAgo = _clock.GetCurrentInstant().Plus(-Duration.FromDays(30));
        var anyEventsFromAMonthAgo = await AnyEventsOlderThanAsync(monthAgo, cancellationToken).ConfigureAwait(false);
        while (anyEventsFromAMonthAgo)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await LogAuditAsync(monthAgo, cancellationToken).ConfigureAwait(false);
            await DeleteOldEventsAsync(monthAgo, cancellationToken).ConfigureAwait(false);
            anyEventsFromAMonthAgo = await AnyEventsOlderThanAsync(monthAgo, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DeleteOldEventsAsync(Instant monthAgo, CancellationToken cancellationToken)
    {
        const string deleteStmt = @"
            WITH CTE AS
             (
                 SELECT TOP 500 *
                 FROM [dbo].[ReceivedIntegrationEvents]
                 WHERE [OccurredOn] < @LastMonthInstant
             )
            DELETE FROM CTE;";

        using var connection = (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.Parameters.AddWithValue("@LastMonthInstant", monthAgo.ToDateTimeUtc());
        command.CommandText = deleteStmt;

        try
        {
            var numberDeletedRecords = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully deleted {NumberDeletedIntegrationEvents} of integration events", numberDeletedRecords);
        }
        catch (DbException e)
        {
            _logger.LogError(e, "Failed to delete old integration events: {ErrorMessage}", e.Message);
            throw;
        }
    }

    private async Task LogAuditAsync(Instant monthAgo, CancellationToken cancellationToken)
    {
        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.RetentionDeletion,
                activityOrigin: nameof(ADayHasPassed),
                activityPayload: monthAgo,
                affectedEntityType: AuditLogEntityType.ReceivedIntegrationEvent,
                affectedEntityKey: null,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> AnyEventsOlderThanAsync(Instant monthAgo, CancellationToken cancellationToken)
    {
        const string selectStmt = @"SELECT CASE WHEN EXISTS (
            SELECT 1 FROM [dbo].[ReceivedIntegrationEvents]
            WHERE [OccurredOn] < @LastMonthInstant
        ) THEN 1 ELSE 0 END";

        using var connection = (SqlConnection)await _databaseConnectionFactory
            .GetConnectionAndOpenAsync(cancellationToken)
            .ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.Parameters.AddWithValue("@LastMonthInstant", monthAgo.ToDateTimeUtc());
        command.CommandText = selectStmt;

        try
        {
            var exists = (int)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Existence of old integration events: {Exists}", exists == 1);
            return exists == 1;
        }
        catch (DbException e)
        {
            _logger.LogError(e, "Failed to get number of old integration events: {ErrorMessage}", e.Message);
            throw;
        }
    }
}
