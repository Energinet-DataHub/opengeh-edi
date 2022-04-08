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
using System.Threading.Tasks;
using B2B.CimMessageAdapter.Messages;
using B2B.Transactions.DataAccess;
using Dapper;

namespace B2B.Transactions.Infrastructure.Messages
{
    public class MessageIdRegistry : IMessageIds
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MessageIdRegistry(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<bool> TryStoreAsync(string? messageId)
        {
            var connection = _connectionFactory.GetOpenConnection();

            var result = await connection.ExecuteAsync(
                    $"IF NOT EXISTS (SELECT * FROM b2b.MessageIds WHERE MessageId = @MessageId)" +
                    $"INSERT INTO b2b.MessageIds(MessageId) VALUES(@MessageId)",
                    new { MessageId = messageId })
                .ConfigureAwait(false);

            return result == 1;
        }
    }
}
