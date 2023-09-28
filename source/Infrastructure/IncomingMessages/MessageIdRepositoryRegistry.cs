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
using Dapper;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages
{
    public class MessageIdRepositoryRegistry : IMessageIdRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ILogger _logger;

        public MessageIdRepositoryRegistry(IDatabaseConnectionFactory connectionFactory, ILogger<MessageIdRepositoryRegistry> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger;
        }

        public async Task StoreAsync(string senderNumber, string messageId, CancellationToken cancellationToken)
        {
            if (senderNumber == null) throw new ArgumentNullException(nameof(senderNumber));

            using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await connection.ExecuteAsync(
                        $"INSERT INTO dbo.MessageRegistry(MessageId, SenderId) VALUES(@MessageId, @SenderId)",
                        new { MessageId = messageId, SenderId = senderNumber })
                    .ConfigureAwait(false);
            }
            catch (SqlException e)
            {
                foreach (SqlError error in e.Errors)
                {
                    if (error.Number == 2627)
                    {
                        _logger.LogError(
                            "Unable to insert message id: {MessageId}" +
                            " for sender: {SenderId} since it already exists in the database",
                            messageId,
                            senderNumber);
                        throw new NotSuccessfulMessageIdStorageException(messageId);
                    }
                }

                throw;
            }
        }

        public async Task<bool> MessageIdExistsAsync(string senderNumber, string messageId, CancellationToken cancellationToken)
        {
            if (senderNumber == null) throw new ArgumentNullException(nameof(senderNumber));

            using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
            var message = await connection.QueryFirstOrDefaultAsync(
                    $"SELECT TOP (1) * FROM dbo.MessageRegistry WHERE MessageId = @MessageId AND SenderId = @SenderId",
                    new { MessageId = messageId, SenderId = senderNumber })
                .ConfigureAwait(false);

            return message != null;
        }
    }
}
