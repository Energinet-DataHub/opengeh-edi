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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure;

public class DequeuedBundlesRetention : IDataRetention
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

    public DequeuedBundlesRetention(IDatabaseConnectionFactory databaseConnectionFactory)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        const string deleteStmt = @"
            DELETE FROM [dbo].[MarketDocuments]
            WHERE [BundleId] IN (SELECT [Id]
            FROM [dbo].[Bundles]
            WHERE [IsDequeued] = 1)

            DELETE FROM [dbo].[OutgoingMessages]
            WHERE [AssignedBundleId] IN (SELECT [Id]
            FROM [dbo].[Bundles]
            WHERE [IsDequeued] = 1)

            DELETE FROM [dbo].[Bundles]
            WHERE [IsDequeued] = 1";

        using var connection =
            (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
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
