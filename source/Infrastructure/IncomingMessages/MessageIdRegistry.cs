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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using CimMessageAdapter.Messages;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IncomingMessages
{
    public class MessageIdRegistry : IMessageIds
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public MessageIdRegistry(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task StoreAsync(string senderId, string messageId, CancellationToken cancellationToken)
        {
            using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await connection.ExecuteAsync(
                        $"INSERT INTO dbo.MessageRegistry(MessageId, SenderId) VALUES(@MessageId, @SenderId)",
                        new { MessageId = messageId, SenderId = senderId })
                    .ConfigureAwait(false);
            }
            catch (SqlException e)
            {
                Console.WriteLine(e);
                throw;
            }

            // var result = await connection.ExecuteAsync(
            //         $"IF NOT EXISTS (SELECT * FROM dbo.MessageRegistry WHERE MessageId = @MessageId AND SenderId = @SenderId)" +
            //         $"INSERT INTO dbo.MessageRegistry(MessageId, SenderId) VALUES(@MessageId, @SenderId)",
            //         new { MessageId = messageId, SenderId = senderId })
            //     .ConfigureAwait(false);
            //
            // if (result != 1)
            // {
            //     throw new DbUpdateException($"Failed to store message id: {messageId}");
            // }
        }

        public async Task<bool> MessageIdIsUniqueForSenderAsync(string senderId, string messageId, CancellationToken cancellationToken)
        {
            using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
            var message = await connection.QueryFirstOrDefaultAsync(
                    $"SELECT * FROM dbo.MessageRegistry WHERE MessageId = @MessageId AND SenderId = @SenderId",
                    new { MessageId = messageId, SenderId = senderId })
                .ConfigureAwait(false);

            return message == null;
        }
    }
}
