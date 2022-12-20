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
using System.Collections.ObjectModel;
using System.Globalization;

namespace Messaging.PerformanceTest.Actors;

public class ActorService : IActorService
{
    private const int NumberOfActors = 100;
    private readonly object _actorNumberLock = new();
    private ConcurrentDictionary<string, bool> _actorNumberDictionary;

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

    public ReadOnlyCollection<string> GetActors()
    {
        lock (_actorNumberLock)
        {
            return new ReadOnlyCollection<string>(_actorNumberDictionary.Where(keyValuePair => keyValuePair.Value).Select(_ => _.Key).ToList());
        }
    }

    public int GetActorCount()
    {
        lock (_actorNumberLock)
        {
            return _actorNumberDictionary.Count;
        }
    }

    public bool IsActorNumberInUse(string actorNumber)
    {
        bool isInUse;
        lock (_actorNumberLock)
        {
            _actorNumberDictionary.TryGetValue(actorNumber, out isInUse);
        }

        return isInUse;
    }

    public void ResetActorNumbers()
    {
        lock (_actorNumberLock)
        {
            _actorNumberDictionary = CreateActorNumbers();
        }
    }

    private static ConcurrentDictionary<string, bool> CreateActorNumbers()
    {
        var actorNumberDictionary = new ConcurrentDictionary<string, bool>();
        const long baseNumber = 7000000000000;

        for (var i = 0; i < NumberOfActors; i++)
        {
            var actorNumber = baseNumber + i;
            actorNumberDictionary.AddOrUpdate(actorNumber.ToString(CultureInfo.InvariantCulture), false, (_, _) => false);
        }

        return actorNumberDictionary;
    }
}
