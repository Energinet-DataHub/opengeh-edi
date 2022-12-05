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
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
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
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);

        var command = CreateCommand($"SELECT Bundle FROM b2b.BundleStore WHERE Id = @Id", new List<KeyValuePair<string, object>>
        {
            new("@Id", GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver)),
        });

        using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
        {
            await reader.ReadAsync().ConfigureAwait(false);
            if (!reader.HasRows)
                return null;

            if (await reader.IsDBNullAsync(0).ConfigureAwait(false))
                return null;

            return reader.GetStream(0);
        }
    }

    public async Task SetBundleForAsync(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver,
        Stream document)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);
        ArgumentNullException.ThrowIfNull(document);

        var command = CreateCommand(
            @$"UPDATE [B2B].[BundleStore]
                     SET Bundle = @Bundle
                     WHERE Id = @Id
                     AND Bundle IS NULL",
            new List<KeyValuePair<string, object>>()
            {
                new("@Id", GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver)),
                new("@Bundle", document),
            });

        var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

        ResetBundleStream(document);

        if (result == 0) throw new BundleException("Fail to store bundle on registration: " + GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver));
    }

    public async Task<bool> TryRegisterBundleAsync(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);

        var bundleRegistrationStatement = $"IF NOT EXISTS (SELECT * FROM b2b.BundleStore WHERE Id = @Id)" +
                                          $"INSERT INTO b2b.BundleStore(Id) VALUES(@Id)";
        var result = await _connectionFactory
            .GetOpenConnection().ExecuteAsync(
                bundleRegistrationStatement,
                new
                {
                    @Id = GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver),
                })
            .ConfigureAwait(false);

        return result == 1;
    }

    private static string GenerateKey(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        return messageCategory.Name + messageReceiverNumber.Value + roleOfReceiver.Name;
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
