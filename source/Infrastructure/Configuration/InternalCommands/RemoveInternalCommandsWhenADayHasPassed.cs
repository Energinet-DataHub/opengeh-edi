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
using Application.Configuration.DataAccess;
using Application.Configuration.TimeEvents;
using MediatR;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Configuration.InternalCommands;

public class RemoveInternalCommandsWhenADayHasPassed : INotificationHandler<ADayHasPassed>
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

    public RemoveInternalCommandsWhenADayHasPassed(IDatabaseConnectionFactory databaseConnectionFactory)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
    }

    public async Task Handle(ADayHasPassed notification, CancellationToken cancellationToken)
    {
        while (await AnyProcessedQueuedInternalCommandsAsync(cancellationToken).ConfigureAwait(false))
        {
            const string deleteStmt = @"
                WITH CTE AS
                 (
                     SELECT TOP 10000 *
                     FROM [dbo].[QueuedInternalCommands]
                      WHERE [ProcessedDate] IS NOT NULL AND [ErrorMessage] IS NULL
                 )
                DELETE FROM CTE;";

            using var connection =
                (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = deleteStmt;

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

    private async Task<bool> AnyProcessedQueuedInternalCommandsAsync(CancellationToken cancellationToken)
    {
        const string selectStmt = @"
               SELECT Count(*) FROM [dbo].[QueuedInternalCommands] WHERE [ProcessedDate] IS NOT NULL AND [ErrorMessage] IS NULL";

        using var connection =
            (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = selectStmt;

        var amountOfProcessedCommands = (int)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return amountOfProcessedCommands > 0;
    }
}
