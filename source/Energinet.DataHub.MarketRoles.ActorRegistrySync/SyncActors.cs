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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.ActorRegistrySync.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync;

public class SyncActors : IDisposable
{
    private readonly ActorSyncService _actorSyncService;
    private readonly B2BSynchronization _b2BSynchronization;
    private readonly EnergySupplyingSynchronization _energySupplyingSynchronization;
    private readonly ActorRegistryDbService _actorRegistry;

    public SyncActors()
    {
        _actorSyncService = new ActorSyncService();
        _actorRegistry =
            new ActorRegistryDbService(Environment.GetEnvironmentVariable("ACTOR_REGISTRY_DB_CONNECTION_STRING") ?? throw new InvalidOperationException());
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                               throw new InvalidOperationException();
        _b2BSynchronization = new B2BSynchronization(connectionString);
        _energySupplyingSynchronization = new EnergySupplyingSynchronization(connectionString);
    }

    [FunctionName("SyncActors")]
    public async Task RunAsync([TimerTrigger("%TIMER_TRIGGER%")] TimerInfo someTimer, ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");
        await SyncActorsFromExternalSourceToDbAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _actorSyncService.Dispose();
            _b2BSynchronization.Dispose();
            _energySupplyingSynchronization.Dispose();
            _actorRegistry.Dispose();
        }
    }

    private async Task SyncActorsFromExternalSourceToDbAsync()
    {
        await SyncEnergySupplyingAsync().ConfigureAwait(false);
        await SyncB2bAsync().ConfigureAwait(false);
    }

    private async Task SyncB2bAsync()
    {
        var actors = (await _actorSyncService.GetActorsAsync().ConfigureAwait(false)).ToList();
        await _b2BSynchronization.SynchronizeAsync(actors).ConfigureAwait(false);
    }

    private async Task SyncEnergySupplyingAsync()
    {
        var actors = (await _actorRegistry.GetLegacyActorsAsync().ConfigureAwait(false)).ToList();
        await _energySupplyingSynchronization.SynchronizeAsync(actors).ConfigureAwait(false);
    }
}
