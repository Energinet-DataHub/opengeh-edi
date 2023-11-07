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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Common.DataAccess;
using Energinet.DataHub.EDI.Common.DataRetention;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Infrastructure.DataRetention;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;

public class ReceivedIntegrationEventsRetention : IDataRetention
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ILogger<ReceivedIntegrationEventsRetention> _logger;

    public ReceivedIntegrationEventsRetention(
        IDatabaseConnectionFactory databaseConnectionFactory,
        ISystemDateTimeProvider systemDateTimeProvider,
        ILogger<ReceivedIntegrationEventsRetention> logger)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _systemDateTimeProvider = systemDateTimeProvider;
        _logger = logger;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var monthAgo = _systemDateTimeProvider.Now().Plus(-Duration.FromDays(30));
        var amountOfOldEvents = await GetAmountOfOldEventsAsync(monthAgo, cancellationToken).ConfigureAwait(false);
        while (amountOfOldEvents > 0)
        {
            const string deleteStmt = @"
                WITH CTE AS
                 (
                     SELECT TOP 500 *
                     FROM [dbo].[ReceivedIntegrationEvents]
                     WHERE [OccurredOn] < @LastMonthInstant
                 )
                DELETE FROM CTE;";

            using var connection =
                (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken)
                    .ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Parameters.AddWithValue(
                "@LastMonthInstant",
                monthAgo.ToDateTimeUtc());
            command.Transaction = transaction;
            command.CommandText = deleteStmt;

            try
            {
                var numberDeletedRecords = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Successfully deleted {NumberDeletedIntegrationEvents} of integration events", numberDeletedRecords);
            }
            catch (DbException e)
            {
                _logger.LogError(e, "Failed to delete old integration events: {ErrorMessage}", e.Message);
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw; // re-throw exception
            }

            amountOfOldEvents = await GetAmountOfOldEventsAsync(monthAgo, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int> GetAmountOfOldEventsAsync(Instant monthAgo, CancellationToken cancellationToken)
    {
        const string selectStmt = @"SELECT Count(*) FROM [dbo].[ReceivedIntegrationEvents]
                     WHERE [OccurredOn] < @LastMonthInstant";

        using var connection =
            (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken)
                .ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.Parameters.AddWithValue(
            "@LastMonthInstant",
            monthAgo.ToDateTimeUtc());
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
