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
using Messaging.Application.Configuration.DataAccess;
using Microsoft.Data.SqlClient;

namespace Messaging.Infrastructure.Configuration.DataAccess
{
    public class SqlDbConnectionFactory : IDbConnectionFactory, IDisposable
    {
        private readonly string _connectionString;
        private IDbConnection _connection = null!;
        private bool _disposed;

        public SqlDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection GetOpenConnection()
        {
            if (_connection is null || _connection.State == ConnectionState.Broken)
            {
                _connection = new SqlConnection(_connectionString);
            }

            if (_connection.State != ConnectionState.Closed)
            {
                _connection.Open();
            }

            return _connection;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _connection?.Dispose();
            _disposed = true;
        }
    }
}
