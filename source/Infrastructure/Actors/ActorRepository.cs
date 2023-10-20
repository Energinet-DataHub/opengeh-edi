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
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Common.Actors;

namespace Energinet.DataHub.EDI.Infrastructure.Actors;

public class ActorRepository : IActorRepository
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

    public ActorRepository(IDatabaseConnectionFactory databaseConnectionFactory)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
    }

    public async Task<Guid> GetIdByActorNumberAsync(string actorNumber, CancellationToken cancellationToken)
    {
        using var connection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection
            .ExecuteScalarAsync<Guid>(
                "SELECT Id FROM [dbo].[Actor] WHERE IdentificationNumber = @Number",
                new { ActorNumber = actorNumber, }).ConfigureAwait(false);
    }

    public async Task<string> GetActorNumberByIdAsync(Guid actorId, CancellationToken cancellationToken)
    {
        using var connection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection
            .ExecuteScalarAsync<string>(
                "SELECT IdentificationNumber FROM [dbo].[Actor] WHERE Id = @ActorId",
                new { ActorId = actorId, }).ConfigureAwait(false);
    }

    public async Task<ActorNumber?> GetActorNumberByB2CIdAsync(Guid actorId, CancellationToken cancellationToken)
    {
        using var connection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        var actorNumber = await connection
            .ExecuteScalarAsync<string>(
                "SELECT IdentificationNumber AS Identifier FROM [dbo].[Actor] WHERE B2CId=@ActorId",
                new { ActorId = actorId, })
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(actorNumber))
        {
            return null;
        }

        return ActorNumber.Create(actorNumber);
    }
}
