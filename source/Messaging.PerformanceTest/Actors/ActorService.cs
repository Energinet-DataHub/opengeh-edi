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

using System.Collections.Concurrent;

namespace Messaging.PerformanceTest.Actors;

public class ActorService : IActorService
{
    private readonly object _actorNumberLock = new();
    private readonly ConcurrentDictionary<string, bool> _actorNumberDictionary;

    public ActorService()
    {
        _actorNumberDictionary = CreateActorNumbers();
    }

    public string? GetUniqueActorNumber()
    {
        lock (_actorNumberLock)
        {
            var actorNumber = _actorNumberDictionary.FirstOrDefault(x => x.Value == false).Key;
            if (actorNumber == null)
                return null;
            _actorNumberDictionary[actorNumber] = true;
            return actorNumber;
        }
    }

    public int GetActorCount()
    {
        lock (_actorNumberLock)
        {
            return _actorNumberDictionary.Count;
        }
    }

    private static ConcurrentDictionary<string, bool> CreateActorNumbers()
    {
        var actorNumberDictionary = new ConcurrentDictionary<string, bool>();
        actorNumberDictionary.AddOrUpdate("45X000000000099K", false, (_, _) => false);
        actorNumberDictionary.AddOrUpdate("7080010006509", false, (_, _) => false);
        actorNumberDictionary.AddOrUpdate("5790002597695", false, (_, _) => false);
        actorNumberDictionary.AddOrUpdate("7080005010788", false, (_, _) => false);
        actorNumberDictionary.AddOrUpdate("5790002602245", false, (_, _) => false);
        actorNumberDictionary.AddOrUpdate("5790001108212", false, (_, _) => false);
        return actorNumberDictionary;
    }
}
