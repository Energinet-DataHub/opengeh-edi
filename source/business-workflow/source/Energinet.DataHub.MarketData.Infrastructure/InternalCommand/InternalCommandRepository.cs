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
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public class InternalCommandRepository : IInternalCommandRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IInternalCommandQuerySettings _querySettings;

        public InternalCommandRepository(IDbConnectionFactory connectionFactory, IInternalCommandQuerySettings querySettings)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _querySettings = querySettings;
        }

        private IDbConnection Connection => _connectionFactory.GetOpenConnection();

        public async Task<IEnumerable<InternalCommand>> GetUnprocessedInternalCommandsInBatchesAsync(int id)
        {
            var query = "SELECT TOP (@BatchSize) [Id], [Command] FROM [dbo].[InternalCommandQueue] I WHERE I.ProcessedDate IS NULL and ScheduledDate <= GETUTCDATE() and I.Id > @Id";
            return await Connection.QueryAsync<InternalCommand>(query, new
            {
                BatchSize = _querySettings.BatchSize,
                Id = id,
            }).ConfigureAwait(false);
        }

        public async Task ProcessInternalCommandAsync(int id)
        {
            await Connection.ExecuteAsync(
                    $"UPDATE InternalCommandQueue SET ProcessedDate = GETDATE() WHERE Id = @Id", param: new
                    {
                        Id = id,
                    })
                .ConfigureAwait(false);
        }
    }
}
