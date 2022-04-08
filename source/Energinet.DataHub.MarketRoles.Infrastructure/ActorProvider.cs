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
using Dapper;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;

namespace Energinet.DataHub.MarketRoles.Infrastructure
{
    public class ActorProvider : IActorProvider
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ActorProvider(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Actor> GetActorAsync(Guid actorId)
        {
            var sql = "SELECT TOP 1 [Id] AS ActorId,[IdentificationType],[IdentificationNumber] AS Identifier,[Roles] FROM [dbo].[Actor] WHERE Id = @ActorId";

            var result = await _connectionFactory
                .GetOpenConnection()
                .QuerySingleOrDefaultAsync<Actor>(sql, new { ActorId = actorId })
                .ConfigureAwait(false);

            return result;
        }
    }
}
