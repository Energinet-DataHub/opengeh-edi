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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.ActorRegistrySync.Entities;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync.Services;

public class ActorSyncService : IDisposable
{
    private readonly ActorRegistryDbService _actorRegistryDbService;
    private readonly MeteringPointDbService _meteringPointDbService;
    private IEnumerable<Actor>? _actors;

    private bool _disposed;

    public ActorSyncService()
    {
        _actorRegistryDbService =
            new ActorRegistryDbService(Environment.GetEnvironmentVariable("ACTOR_REGISTRY_DB_CONNECTION_STRING") ?? throw new InvalidOperationException());
        _meteringPointDbService = new MeteringPointDbService(
            Environment.GetEnvironmentVariable("METERINGPOINT_DB_CONNECTION_STRING") ??
            throw new InvalidOperationException());
    }

    public async Task DatabaseCleanUpAsync()
    {
        await _meteringPointDbService.CleanUpAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<UserActor>> GetUserActorsAsync()
    {
       return await _meteringPointDbService.GetUserActorsAsync().ConfigureAwait(false);
    }

    public async Task InsertUserActorsAsync(IEnumerable<UserActor> userActors)
    {
        if (_actors != null)
        {
            var userActorsToInsert = userActors.Where(u => _actors.Any(actor => u.ActorId == actor.Id));
            await _meteringPointDbService.InsertUserActorsAsync(userActorsToInsert).ConfigureAwait(false);
        }
    }

    public async Task SyncActorsAsync()
    {
        _actors = await _actorRegistryDbService.GetActorsAsync().ConfigureAwait(false);
        await _meteringPointDbService.InsertActorsAsync(_actors).ConfigureAwait(false);
    }

    public async Task SyncGridAreaLinksAsync()
    {
        var gridAreaLinks = await _actorRegistryDbService.GetGriAreaLinkAsync().ConfigureAwait(false);
        await _meteringPointDbService.InsertGriAreaLinkAsync(gridAreaLinks).ConfigureAwait(false);
    }

    public async Task SyncGridAreasAsync()
    {
        var gridAreas = await _actorRegistryDbService.GetGridAreasAsync().ConfigureAwait(false);
        await _meteringPointDbService.InsertGridAreasAsync(gridAreas).ConfigureAwait(false);
    }

    public async Task CommitTransactionAsync()
    {
        await _meteringPointDbService.CommitTransactionAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _actorRegistryDbService.Dispose();
            _meteringPointDbService.Dispose();
        }

        _disposed = true;
    }
}
