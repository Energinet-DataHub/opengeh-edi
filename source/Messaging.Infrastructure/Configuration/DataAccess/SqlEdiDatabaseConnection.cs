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
using System.Data;
using System.Threading.Tasks;
using Messaging.Application.Configuration.DataAccess;
using Microsoft.Data.SqlClient;

namespace Messaging.Infrastructure.Configuration.DataAccess
{
    public class SqlEdiDatabaseConnection : IEdiDatabaseConnection
    {
        private readonly string _connectionString;

        public SqlEdiDatabaseConnection(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection GetConnectionAndOpen()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();

            return connection;
        }

        public async ValueTask<IDbConnection> GetConnectionAndOpenAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            return connection;
        }

        public IDbConnection GetConnection()
            => new SqlConnection(_connectionString);
    }
}
