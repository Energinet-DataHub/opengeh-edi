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
using Messaging.Application.Actors;
using Messaging.Application.Configuration.DataAccess;

namespace Messaging.Infrastructure.Actors;

public class ActorRegistry : IActorRegistry
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ActorRegistry(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task TryStoreAsync(CreateActor createActor)
    {
        if (createActor == null) throw new ArgumentNullException(nameof(createActor));
        var connection = _dbConnectionFactory.GetOpenConnection();
        var sqlStatement = @$"INSERT INTO [b2b].[Actor] ([Id],[IdentificationNumber]) VALUES ('{createActor.ActorId}', '{createActor.IdentificationNumber}')";
        await connection.ExecuteAsync(sqlStatement)
            .ConfigureAwait(false);
    }
}
