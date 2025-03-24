﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration;

public sealed class SqlConnectionSource : IDisposable
{
    public SqlConnectionSource(IOptions<SqlDatabaseConnectionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var builder = new SqlConnectionStringBuilder(options.Value.DB_CONNECTION_STRING)
        {
            Encrypt = true,
        };
        Connection = new SqlConnection(builder.ConnectionString);
    }

    public SqlConnection Connection { get;  }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connection.Dispose();
        }
    }
}
