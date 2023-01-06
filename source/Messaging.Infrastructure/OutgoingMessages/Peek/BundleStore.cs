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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages.Dequeue;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Domain.OutgoingMessages;
using Microsoft.Data.SqlClient;

namespace Messaging.Infrastructure.OutgoingMessages.Peek;

public class BundleStore : IBundleStore
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public BundleStore(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> SetBundleForAsync(
        BundleId bundleId,
        Stream document,
        Guid messageId,
        IEnumerable<Guid> messageIdsIncluded)
    {
        ArgumentNullException.ThrowIfNull(bundleId);
        ArgumentNullException.ThrowIfNull(document);

        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        var insertStatement =
            $"IF NOT EXISTS (SELECT * FROM b2b.BundleStore WHERE ActorNumber = @ActorNumber AND MessageCategory = @MessageCategory)" +
            $"INSERT INTO b2b.BundleStore(ActorNUmber, MessageCategory, MessageId, MessageIdsIncluded, Bundle) VALUES(@ActorNumber, @MessageCategory, @MessageId, @MessageIdsIncluded, @Bundle)";
        using var command = CreateCommand(
            insertStatement,
            new List<KeyValuePair<string, object>>()
            {
                new("@ActorNumber", bundleId.ReceiverNumber.Value),
                new("@MessageCategory", bundleId.MessageCategory.Name),
                new("@Bundle", document),
                new("@MessageId", messageId),
                new("@MessageIdsIncluded", string.Join(",", messageIdsIncluded)),
            },
            connection);

        var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        ResetBundleStream(document);

        return result == 1;
        //if (result == 0) throw new BundleException($"Fail to store bundle on registration: {bundleId.MessageCategory.Name}, {bundleId.ReceiverNumber.Value}");
    }

    public async Task<bool> TryRegisterBundleAsync(
        BundleId bundleId)
    {
        ArgumentNullException.ThrowIfNull(bundleId);

        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        var result = await connection.ExecuteAsync(
                $"IF NOT EXISTS (SELECT * FROM b2b.BundleStore WHERE ActorNumber = @ActorNumber AND MessageCategory = @MessageCategory)" +
                $"INSERT INTO b2b.BundleStore(ActorNUmber, MessageCategory) VALUES(@ActorNumber, @MessageCategory)",
                new
                {
                    @ActorNumber = bundleId.ReceiverNumber.Value,
                    @MessageCategory = bundleId.MessageCategory.Name,
                })
            .ConfigureAwait(false);

        return result == 1;
    }

    public async Task UnregisterBundleAsync(BundleId bundleId)
    {
        ArgumentNullException.ThrowIfNull(bundleId);
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        await connection
            .ExecuteAsync(
                $"DELETE FROM b2b.BundleStore WHERE ActorNumber = @ActorNumber AND MessageCategory = @MessageCategory",
                new
                {
                    @ActorNumber = bundleId.ReceiverNumber.Value,
                    @MessageCategory = bundleId.MessageCategory.Name,
                }).ConfigureAwait(false);
    }

    public async Task<DequeueResult> DequeueAsync(Guid messageId)
    {
        const string deleteStmt = @"
DELETE E FROM [b2b].[EnqueuedMessages] E JOIN
(SELECT EnqueuedMessageId = value FROM [b2b].[BundleStore]
CROSS APPLY STRING_SPLIT(MessageIdsIncluded, ',') WHERE MessageId = @messageId) AS P
ON E.Id = P.EnqueuedMessageId;

DELETE FROM [b2b].[BundleStore] WHERE MessageId = @messageId;
";

        using var connection = (SqlConnection)await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = deleteStmt;
        command.Parameters.Add(new SqlParameter("messageId", SqlDbType.UniqueIdentifier) { Value = messageId });
        int result;

        try
        {
            result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
        }
        catch (DbException)
        {
            // Add exception logging
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw; // re-throw exception
        }

        return result > 0 ? new DequeueResult(true) : new DequeueResult(false);
    }

    public async Task<Guid?> GetBundleMessageIdOfAsync(BundleId bundleId)
    {
        ArgumentNullException.ThrowIfNull(bundleId);
        const string sqlquery = @"SELECT MessageId FROM [b2b].[BundleStore] WHERE ActorNumber = @ActorNumber AND MessageCategory = @MessageCategory";

        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        var messageId = await connection.QueryFirstOrDefaultAsync<Guid?>(sqlquery, new { ActorNumber = bundleId.ReceiverNumber.Value, MessageCategory = bundleId.MessageCategory.Name }).ConfigureAwait(false);
        return messageId;
    }

    private static async Task<bool> HasBundleRegisteredAsync(SqlDataReader reader)
    {
        if (!reader.HasRows)
            return false;

        return !await reader.IsDBNullAsync(0).ConfigureAwait(false);
    }

    private static void ResetBundleStream(Stream document)
    {
        document.Position = 0;
    }

    private static SqlCommand CreateCommand(string sqlStatement, List<KeyValuePair<string, object>> parameters, IDbConnection connection)
    {
        var command = connection.CreateCommand();

        command.CommandText = sqlStatement;

        foreach (var parameter in parameters)
        {
            var sqlParameter = new SqlParameter(parameter.Key, parameter.Value);
            command.Parameters.Add(sqlParameter);
        }

        return (SqlCommand)command;
    }
}
