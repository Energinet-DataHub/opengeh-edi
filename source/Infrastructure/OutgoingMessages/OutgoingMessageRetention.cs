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
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.DataRetention;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.Infrastructure.OutgoingMessages;

public class OutgoingMessageRetention : IDataRetention
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ILogger<OutgoingMessageRetention> _logger;

    public OutgoingMessageRetention(
        IDatabaseConnectionFactory databaseConnectionFactory,
        ISystemDateTimeProvider systemDateTimeProvider,
        ILogger<OutgoingMessageRetention> logger)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _systemDateTimeProvider = systemDateTimeProvider;
        _logger = logger;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var monthAgo = _systemDateTimeProvider.Now().Plus(-Duration.FromDays(30));
        const string deleteStmt = @"
            DELETE FROM [dbo].[MarketDocuments]
            WHERE [BundleId] IN (SELECT [Id]
            FROM [dbo].[Bundles]
            WHERE [IsDequeued] = 1)

            DELETE FROM [dbo].[Bundles]
            WHERE [IsDequeued] = 1";

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
