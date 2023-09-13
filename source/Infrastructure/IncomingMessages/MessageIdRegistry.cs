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
using CimMessageAdapter.Messages.Exceptions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IncomingMessages
{
    public class MessageIdRegistry : IMessageIds
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ILogger _logger;

        public MessageIdRegistry(IDatabaseConnectionFactory connectionFactory, ILogger<MessageIdRegistry> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger;
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
                for (var index = 0; index < e.Errors.Count; index++)
                {
                    if (e.Errors[index].Number == 2627)
                    {
                        _logger.LogError(
                            "Unable to insert message id: {MessageId}" +
                            " for sender: {SenderId} since it already exists in the database",
                            messageId,
                            senderId);
                        throw new UnsuccessfulMessageIdStorageException();
                    }
                }

                throw;
            }
        }

        public async Task<bool> MessageIdIsUniqueForSenderAsync(string senderId, string messageId, CancellationToken cancellationToken)
        {
            using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
            var message = await connection.QueryFirstOrDefaultAsync(
                    $"SELECT TOP (1) * FROM dbo.MessageRegistry WHERE MessageId = @MessageId AND SenderId = @SenderId",
                    new { MessageId = messageId, SenderId = senderId })
                .ConfigureAwait(false);

            return message == null;
        }
    }
}
