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

using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.MasterData.Domain.Actors;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Actor = Energinet.DataHub.EDI.MasterData.Domain.Actors.Actor;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.Actors;

public class ActorRepository : IActorRepository
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly MasterDataContext _masterDataContext;

    public ActorRepository(IDatabaseConnectionFactory databaseConnectionFactory, MasterDataContext masterDataContext)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _masterDataContext = masterDataContext;
    }

    public async Task<ActorNumber?> GetActorNumberByExternalIdAsync(string externalId, CancellationToken cancellationToken)
    {
        using var connection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        var actorNumber = await connection
            .ExecuteScalarAsync<string>(
                "SELECT ActorNumber AS Identifier FROM [dbo].[Actor] WHERE ExternalId=@ExternalId",
                new { ExternalId = externalId, })
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(actorNumber))
        {
            return null;
        }

        return ActorNumber.Create(actorNumber);
    }

    public async Task CreateIfNotExistAsync(ActorNumber actorNumber, string externalId, CancellationToken cancellationToken)
    {
        if (await ActorDoesNotExistsAsync(actorNumber, externalId, cancellationToken).ConfigureAwait(false))
        {
            await _masterDataContext.Actors
                .AddAsync(new Actor(actorNumber, externalId), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<bool> ActorDoesNotExistsAsync(ActorNumber actorNumber, string externalId, CancellationToken cancellationToken)
    {
        return !await _masterDataContext.Actors
            .AnyAsync(
                actor => actor.ActorNumber == actorNumber
                               && actor.ExternalId == externalId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
