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
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages.Dequeue;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Microsoft.Data.SqlClient;

namespace Messaging.Application.OutgoingMessages.Peek;

public class BundleStore
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BundleStore(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Stream?> GetBundleOfAsync(
        BundleId bundleId)
    {
        ArgumentNullException.ThrowIfNull(bundleId);

        var command = CreateCommand($"SELECT Bundle FROM b2b.BundleStore WHERE ActorNumber = @ActorNumber AND ActorRole = @ActorRole AND MessageCategory = @MessageCategory", new List<KeyValuePair<string, object>>
        {
            new("@ActorNumber", bundleId.ActorNumber.Value),
            new("@ActorRole", bundleId.MarketRole.Name),
            new("@MessageCategory", bundleId.MessageCategory.Name),
        });

        using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
        {
            await reader.ReadAsync().ConfigureAwait(false);
            if (await HasBundleRegisteredAsync(reader).ConfigureAwait(false) == false)
                return null;

            return reader.GetStream(0);
        }
    }

    public async Task SetBundleForAsync(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver,
        Stream document,
        Guid messageId,
        IEnumerable<Guid> messageIdsIncluded)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);
        ArgumentNullException.ThrowIfNull(document);

        var command = CreateCommand(
            @$"UPDATE [B2B].[BundleStore]
                     SET Bundle = @Bundle, MessageId = @MessageId, MessageIdsIncluded = @MessageIdsIncluded
                     WHERE ActorNumber = @ActorNumber
                     AND  ActorRole = @ActorRole
                     AND MessageCategory = @MessageCategory
                     AND Bundle IS NULL
                     AND MessageId IS NULL",
            new List<KeyValuePair<string, object>>()
            {
                new("@ActorNumber", messageReceiverNumber.Value),
                new("@ActorRole", roleOfReceiver.Name),
                new("@MessageCategory", messageCategory.Name),
                new("@Bundle", document),
                new("@MessageId", messageId),
                new("@MessageIdsIncluded", string.Join(",", messageIdsIncluded)),
            });

        var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

        ResetBundleStream(document);

        if (result == 0) throw new BundleException($"Fail to store bundle on registration: {messageCategory.Name}, {messageReceiverNumber.Value}, {roleOfReceiver.Name}");
    }

    public async Task<bool> TryRegisterBundleAsync(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);

        var result = await _connectionFactory
            .GetOpenConnection().ExecuteAsync(
                $"IF NOT EXISTS (SELECT * FROM b2b.BundleStore WHERE ActorNumber = @ActorNumber AND ActorRole = @ActorRole AND MessageCategory = @MessageCategory)" +
                $"INSERT INTO b2b.BundleStore(ActorNUmber, ActorRole, MessageCategory) VALUES(@ActorNumber, @ActorRole, @MessageCategory)",
                new
                {
                    @ActorNumber = messageReceiverNumber.Value,
                    @ActorRole = roleOfReceiver.Name,
                    @MessageCategory = messageCategory.Name,
                })
            .ConfigureAwait(false);

        return result == 1;
    }

    public async Task<DequeueResult> DequeueAsync(Guid messageId)
    {
        var bundleStoreQuery = await _connectionFactory.GetOpenConnection().QuerySingleOrDefaultAsync(
            $"SELECT * FROM [B2B].[BundleStore] WHERE MessageId = @MessageId",
            new
        {
            MessageId = messageId,
        }).ConfigureAwait(false);
        if (bundleStoreQuery is null)
            return new DequeueResult(false);
        string messageIdIncluded = bundleStoreQuery.MessageIdsIncluded;
        string actorNumber = bundleStoreQuery.ActorNumber;
        var statementBuilder = new StringBuilder();
        foreach (var id in messageIdIncluded.Split(","))
        {
            var deleteMessageRowSql = $"DELETE FROM [B2B].ActorMessageQueue_{actorNumber} WHERE Id = '{id}';";
            statementBuilder.AppendLine(deleteMessageRowSql);
        }

        var deleteBundleStoreRowSql = $"DELETE FROM [B2B].[BundleStore] WHERE MessageId = @MessageId";
        statementBuilder.AppendLine(deleteBundleStoreRowSql);
        var command = CreateCommand(
            statementBuilder.ToString(),
            new List<KeyValuePair<string, object>>()
            {
                new("@MessageId", messageId),
            });
        var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

        return result > 0 ? new DequeueResult(true) : new DequeueResult(false);
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

    private SqlCommand CreateCommand(string sqlStatement, List<KeyValuePair<string, object>> parameters)
    {
        var command = _connectionFactory
            .GetOpenConnection()
            .CreateCommand();

        command.CommandText = sqlStatement;

        foreach (var parameter in parameters)
        {
            var sqlParameter = new SqlParameter(parameter.Key, parameter.Value);
            command.Parameters.Add(sqlParameter);
        }

        return (SqlCommand)command;
    }
}
