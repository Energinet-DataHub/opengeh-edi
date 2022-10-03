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
    private readonly MessagingSynchronization _messagingSynchronization;
    private readonly EnergySupplyingSynchronization _energySupplyingSynchronization;
    private readonly ActorRegistryDbService _actorRegistry;

    public SyncActors()
    {
        _actorRegistry =
            new ActorRegistryDbService(Environment.GetEnvironmentVariable("ACTOR_REGISTRY_DB_CONNECTION_STRING") ?? throw new InvalidOperationException());
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                               throw new InvalidOperationException();
        _messagingSynchronization = new MessagingSynchronization(connectionString);
        _energySupplyingSynchronization = new EnergySupplyingSynchronization(connectionString);
    }

    [FunctionName("SyncActors")]
    public async Task RunAsync([TimerTrigger("%TIMER_TRIGGER%")] TimerInfo someTimer, ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");
        await SyncEnergySupplyingAsync().ConfigureAwait(false);
        await SyncB2BAsync().ConfigureAwait(false);
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
            _messagingSynchronization.Dispose();
            _energySupplyingSynchronization.Dispose();
            _actorRegistry.Dispose();
        }
    }

    private async Task SyncB2BAsync()
    {
        var actors = (await _actorRegistry.GetActorsAsync().ConfigureAwait(false)).ToList();
        await _messagingSynchronization.SynchronizeAsync(actors).ConfigureAwait(false);
    }

    private async Task SyncEnergySupplyingAsync()
    {
        var actors = (await _actorRegistry.GetActorsAsync().ConfigureAwait(false)).ToList();
        await _energySupplyingSynchronization.SynchronizeAsync(actors).ConfigureAwait(false);
    }
}
