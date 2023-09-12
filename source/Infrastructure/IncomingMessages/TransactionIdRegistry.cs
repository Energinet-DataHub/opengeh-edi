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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.Exceptions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Infrastructure.IncomingMessages
{
    public class TransactionIdRegistry : ITransactionIds
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public TransactionIdRegistry(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<bool> TransactionIdOfSenderIsUniqueAsync(string senderId, string transactionId, CancellationToken cancellationToken)
        {
            using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
            var transaction = await connection.QueryFirstOrDefaultAsync(
                    $"SELECT * FROM dbo.TransactionRegistry WHERE TransactionId = @TransactionId AND SenderId = @SenderId",
                    new { TransactionId = transactionId, SenderId = senderId })
                .ConfigureAwait(false);

            return transaction == null;
        }

        public async Task<bool> StoreTransactionIdsForSenderAsync(
            string senderId,
            IReadOnlyList<string> transactionIds,
            CancellationToken cancellationToken)
        {
            if (transactionIds == null) throw new ArgumentNullException(nameof(transactionIds));

            const string insertStmt = @"
                INSERT INTO dbo.TransactionRegistry (TransactionId, SenderId) VALUES (@TransactionId, @SenderId)";

            using var connection =
                (SqlConnection)await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = insertStmt;

            try
            {
                foreach (var transactionId in transactionIds)
                {
                    command.Parameters.Clear();
                    command.Parameters.Add(
                        "@TransactionId", SqlDbType.NChar).Value = transactionId;
                    command.Parameters.Add(
                        "@SenderId", SqlDbType.NChar).Value = senderId;
                    await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new UnsuccessfulTransactionIdsStorageException();
            }

            return true;
        }
    }
}
