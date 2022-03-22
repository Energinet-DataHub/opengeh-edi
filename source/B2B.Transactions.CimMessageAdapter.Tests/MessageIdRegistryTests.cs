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
using B2B.CimMessageAdapter.Message.MessageIds;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Xunit;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests
{
    public class MessageIdRegistryTests
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IMessageIds _messageIds;

        public MessageIdRegistryTests()
        {
            _connectionFactory = new SqlDbConnectionFactory(
                "Server=(local); Database=sqldb-marketroles-endk;Trusted_connection=true;MultipleActiveResultSets=True");
            _messageIds = new MessageIdRegistry(_connectionFactory);
        }

        [Fact]
        public async Task Has_no_duplicate_message_id()
        {
            var messageId = Guid.NewGuid().ToString();

            var result = await _messageIds.TryStoreAsync(messageId).ConfigureAwait(false);

            Assert.True(result);
        }

        [Fact]
        public async Task Has_duplicate_message_id()
        {
            var messageId = Guid.NewGuid().ToString();
            await _messageIds.TryStoreAsync(messageId).ConfigureAwait(false);

            var result = await _messageIds.TryStoreAsync(messageId).ConfigureAwait(false);

            Assert.False(result);
        }
    }
}
