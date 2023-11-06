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

using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Common.DataAccess;
using Energinet.DataHub.EDI.Domain.ArchivedMessages;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.EDI.Infrastructure.ArchivedMessages;

public class ArchivedMessageRepository : IArchivedMessageRepository
{
    private readonly B2BContext _dbContext;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public ArchivedMessageRepository(B2BContext dbContext, IDatabaseConnectionFactory connectionFactory)
    {
        _dbContext = dbContext;
        _connectionFactory = connectionFactory;
    }

    public void Add(ArchivedMessage message)
    {
        _dbContext.ArchivedMessages.Add(message);
    }

    public async Task<Stream?> GetAsync(string id, CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = CreateCommand(
            $"SELECT Document FROM dbo.[ArchivedMessages] WHERE Id = @Id",
            new List<KeyValuePair<string, object>>
            {
                new("@Id", id),
            },
            connection);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (reader.HasRows == false)
        {
            return null;
        }

        return reader.GetStream(0);
    }

    private static SqlCommand CreateCommand(
        string sqlStatement, List<KeyValuePair<string, object>> parameters, IDbConnection connection)
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
