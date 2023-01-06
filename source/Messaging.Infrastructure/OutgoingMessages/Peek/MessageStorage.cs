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
using System.IO;
using System.Threading.Tasks;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Domain.OutgoingMessages;
using Microsoft.Data.SqlClient;

namespace Messaging.Infrastructure.OutgoingMessages.Peek;

public class MessageStorage : IMessageStorage
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public MessageStorage(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public virtual async Task<Stream?> GetMessageOfAsync(BundleId bundleId)
    {
        ArgumentNullException.ThrowIfNull(bundleId);
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);

        using var command = CreateCommand(
            $"SELECT Bundle FROM b2b.BundleStore WHERE ActorNumber = @ActorNumber AND MessageCategory = @MessageCategory",
            new List<KeyValuePair<string, object>>
            {
                new("@ActorNumber", bundleId.ReceiverNumber.Value),
                new("@MessageCategory", bundleId.MessageCategory.Name),
            },
            connection);

        using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
        {
            await reader.ReadAsync().ConfigureAwait(false);
            if (await HasBundleRegisteredAsync(reader).ConfigureAwait(false) == false)
                return null;

            return reader.GetStream(0);
        }
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

    private static async Task<bool> HasBundleRegisteredAsync(SqlDataReader reader)
    {
        if (!reader.HasRows)
            return false;

        return !await reader.IsDBNullAsync(0).ConfigureAwait(false);
    }
}
