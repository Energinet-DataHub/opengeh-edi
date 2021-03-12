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
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence
{
    /// <summary>
    /// Long lived implementation where the <see cref="DbTransaction"/> is established on the first command or query operation
    /// <remarks>
    /// The transaction is created on the first database interaction, eiter <see cref="QueryAsync{T}"/> or <see cref="CompleteAsync"/>
    /// All <see cref="IAsyncCommand"/> operations are written immediately to the database in the transaction.
    /// When <see cref="CompleteAsync"/> the underlying transaction is committed. If <see cref="AbortAsync"/> is invoked
    /// a rollback is issued to the transaction.
    /// </remarks>
    /// </summary>
    public sealed class LongLivedDbTransaction : IUnitOfWork, IDataWriter, IDisposable
    {
        private readonly Func<DbConnection> _connectionFactory;
        private DbConnection? _dbConnection;
        private DbTransaction? _dbTransaction;

        public LongLivedDbTransaction(Func<DbConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellationToken = default)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            await EnsureConnectionIsEstablishedAsync(cancellationToken).ConfigureAwait(false);
            if (_dbConnection == null) throw new InvalidOperationException("No db connection established");
            if (_dbTransaction == null) throw new InvalidOperationException("No db transaction established");

            await command.ExecuteNonQueryAsync(
                    _dbConnection,
                    _dbTransaction,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<T> QueryAsync<T>(IAsyncQuery<T> query, CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            await EnsureConnectionIsEstablishedAsync(cancellationToken).ConfigureAwait(false);
            if (_dbConnection == null) throw new InvalidOperationException("No db connection established");
            if (_dbTransaction == null) throw new InvalidOperationException("No db transaction established");

            return await query.ExecuteQueryAsync(
                    _dbConnection,
                    _dbTransaction,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (_dbTransaction != null) await _dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            Reset();
        }

        public async Task AbortAsync(CancellationToken cancellationToken = default)
        {
            if (_dbTransaction != null) await _dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            Reset();
        }

        public void Dispose()
        {
            _dbConnection?.Dispose();
            _dbTransaction?.Dispose();

            GC.SuppressFinalize(this);
        }

        private async ValueTask EnsureConnectionIsEstablishedAsync(CancellationToken cancellationToken = default)
        {
            if (_dbConnection != null) return;
            _dbConnection = _connectionFactory.Invoke();

            await _dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            _dbTransaction = await _dbConnection
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);
        }

        private void Reset()
        {
            Dispose();

            _dbConnection = null;
            _dbTransaction = null;
        }
    }
}
