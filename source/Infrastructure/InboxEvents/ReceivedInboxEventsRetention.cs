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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration;
using Application.Configuration.DataAccess;
using Infrastructure.DataRetention;
using Microsoft.Data.SqlClient;
using NodaTime;

namespace Infrastructure.InboxEvents;

public class ReceivedInboxEventsRetention : IDataRetention
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public ReceivedInboxEventsRetention(
        IDatabaseConnectionFactory databaseConnectionFactory,
        ISystemDateTimeProvider systemDateTimeProvider)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var monthAgo = _systemDateTimeProvider.Now().Plus(-Duration.FromDays(30));
        var amountOfOldEvents = 1;
        while (amountOfOldEvents > 0)
        {
            const string deleteStmt = @"
                WITH CTE AS
                 (
                     SELECT TOP 10000 *
                     FROM [dbo].[ReceivedInboxEvents]
                     WHERE [ErrorMessage] IS NULL AND [ProcessedDate] IS NOT NULL AND [ProcessedDate] < @LastMonthInstant
                 )
                DELETE FROM CTE;

                SELECT Count(*) FROM [dbo].[ReceivedInboxEvents]
                    WHERE [ErrorMessage] IS NULL AND [ProcessedDate] IS NOT NULL AND [ProcessedDate] < @LastMonthInstant";

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
                amountOfOldEvents = (int)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbException)
            {
                // Add exception logging
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw; // re-throw exception
            }
        }
    }
}
