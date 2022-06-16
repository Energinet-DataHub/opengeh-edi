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
    private readonly MarketRolesDbService _marketRolesDbService;

    private bool _disposed;

    public ActorSyncService()
    {
        _actorRegistryDbService =
            new ActorRegistryDbService(Environment.GetEnvironmentVariable("ACTOR_REGISTRY_DB_CONNECTION_STRING") ?? throw new InvalidOperationException());
        _marketRolesDbService = new MarketRolesDbService(
            Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
            throw new InvalidOperationException());
    }

    public async Task DatabaseCleanUpAsync()
    {
        await _marketRolesDbService.CleanUpAsync().ConfigureAwait(false);
    }

    public async Task SyncActorsAsync()
    {
        var actors = await _actorRegistryDbService.GetActorsAsync().ConfigureAwait(false);
        await _marketRolesDbService.InsertActorsAsync(actors).ConfigureAwait(false);
    }

    public async Task SyncEnergySuppliersAsync()
    {
        var actors = await _actorRegistryDbService.GetEnergySuppliersAsync().ConfigureAwait(false);
        await _marketRolesDbService.InsertEnergySuppliersAsync(actors).ConfigureAwait(false);
    }

    public async Task CommitTransactionAsync()
    {
        await _marketRolesDbService.CommitTransactionAsync().ConfigureAwait(false);
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
            _marketRolesDbService.Dispose();
        }

        _disposed = true;
    }
}
