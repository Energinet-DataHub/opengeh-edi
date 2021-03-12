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
using System.Threading;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence
{
    /// <summary>
    /// Ensures that the database transaction is open in the shortest period of time
    /// <remarks>
    /// The transaction is created when the <see cref="CompleteAsync"/> is invoked.
    /// All the commands are stored in memory and waiting to be executed.
    /// A consequence of this design is that a query to the database would not receive
    /// results from a pending <see cref="IAsyncCommand"/>. After <see cref="CompleteAsync"/>
    /// or <see cref="AbortAsync"/> all pending commands are cleared.
    /// </remarks>
    /// </summary>
    public sealed class ShortLivedDbTransaction : IUnitOfWork, IDataWriter, IDisposable
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly List<IAsyncCommand> _pendingCommands = new List<IAsyncCommand>();
        private DbConnection? _dbConnection;

        public ShortLivedDbTransaction(Func<DbConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellationToken = default)
        {
            _pendingCommands.Add(command);
            return Task.CompletedTask;
        }

        public async Task<T> QueryAsync<T>(IAsyncQuery<T> query, CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            await EnsureConnectionIsEstablishedAsync(cancellationToken);
            if (_dbConnection == null) throw new InvalidOperationException("No db connection established");

            return await query
                .ExecuteQueryAsync(_dbConnection, null, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            await EnsureConnectionIsEstablishedAsync(cancellationToken);
            if (_dbConnection == null) throw new InvalidOperationException("Unable to establish connection");

            try
            {
                await using var transactionScope = await _dbConnection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
                try
                {
                    foreach (var pendingCommand in _pendingCommands)
                    {
                        await pendingCommand
                            .ExecuteNonQueryAsync(_dbConnection, transactionScope, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    await transactionScope.CommitAsync(cancellationToken);
                }
                catch (Exception)
                {
                    await transactionScope.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
            finally
            {
                Reset();
            }
        }

        public Task AbortAsync(CancellationToken cancellationToken = default)
        {
            Reset();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _dbConnection?.Dispose();
        }

        private async ValueTask EnsureConnectionIsEstablishedAsync(CancellationToken cancellationToken = default)
        {
            if (_dbConnection != null) return;
            _dbConnection = _connectionFactory.Invoke();

            await _dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        private void Reset()
        {
            Dispose();

            _dbConnection = null;
            _pendingCommands.Clear();
        }
    }
}
