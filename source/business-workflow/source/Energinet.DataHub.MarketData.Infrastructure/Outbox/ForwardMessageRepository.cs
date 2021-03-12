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
using Dapper;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;

namespace Energinet.DataHub.MarketData.Infrastructure.Outbox
{
    public class ForwardMessageRepository : IForwardMessageRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ForwardMessageRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private IDbConnection Connection => _connectionFactory.GetOpenConnection();

        public async Task<ForwardMessage?> GetUnprocessedForwardMessageAsync()
        {
            var query = "SELECT TOP(1) * FROM [dbo].[OutgoingActorMessages] O WHERE O.State = @State";
            return await Connection.QueryFirstOrDefaultAsync<ForwardMessage>(query, new
            {
                State = OutboxState.Pending.Id,
            }).ConfigureAwait(false);
        }

        public async Task MarkForwardedMessageAsProcessedAsync(int id)
        {
            await Connection.ExecuteAsync(
                    $"UPDATE OutgoingActorMessages SET LastUpdatedOn = GETDATE(), State = @State WHERE Id = @Id", param: new
                    {
                        State = OutboxState.Processed.Id,
                        Id = id,
                    })
                .ConfigureAwait(false);
        }
    }
}
