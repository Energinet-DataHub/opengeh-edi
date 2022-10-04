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

using Messaging.Domain.SeedWork;

namespace Messaging.Domain.Actors;

public class ActorNumber : ValueObject
{
    private ActorNumber(string actorNumber)
    {
        Value = actorNumber;
    }

    public string Value { get; }

    public static ActorNumber Create(string actorNumber)
    {
        if (actorNumber == null) throw new ArgumentNullException(nameof(actorNumber));
        return IsGlnNumber(actorNumber) || IsEic(actorNumber)
            ? new ActorNumber(actorNumber)
            : throw InvalidActorNumberException.Create(actorNumber);
    }

    private static bool IsEic(string actorNumber)
    {
        return actorNumber.Length == 16;
    }

    private static bool IsGlnNumber(string actorNumber)
    {
        return actorNumber.Length == 13;
    }
}
