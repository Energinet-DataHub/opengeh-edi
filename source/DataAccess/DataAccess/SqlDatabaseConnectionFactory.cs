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

using System.Data;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.DataAccess.DataAccess;

public class SqlDatabaseConnectionFactory : IDatabaseConnectionFactory
{
    private readonly SqlDatabaseConnectionOptions _options;

    public SqlDatabaseConnectionFactory(IOptions<SqlDatabaseConnectionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public IDbConnection GetConnectionAndOpen()
    {
        var connection = new SqlConnection(_options.DB_CONNECTION_STRING);
        connection.Open();

        return connection;
    }

    public async ValueTask<IDbConnection> GetConnectionAndOpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_options.DB_CONNECTION_STRING);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }
}
