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

using System.Collections.Generic;
using System.Threading.Tasks;
using Messaging.Application.Transactions.Aggregations;
using Messaging.Domain.Actors;

namespace Messaging.Infrastructure.Transactions.Aggregations;

public class FakeGridAreaLookup : IGridAreaLookup
{
    private readonly Dictionary<string, ActorNumber> _gridAreas = new()
    {
        { "805", ActorNumber.Create("8200000007739") },
        { "806", ActorNumber.Create("8200000007746") },
    };

    public Task<ActorNumber> GetGridOperatorForAsync(string gridAreaCode)
    {
        return Task.FromResult(_gridAreas[gridAreaCode]);
    }
}
