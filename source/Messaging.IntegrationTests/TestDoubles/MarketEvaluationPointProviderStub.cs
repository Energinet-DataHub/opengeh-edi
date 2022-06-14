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
using Messaging.Application.Common;

namespace Messaging.IntegrationTests.TestDoubles;

public class MarketEvaluationPointProviderStub : IMarketEvaluationPointProvider
{
    private readonly List<MarketEvaluationPoint> _marketEvaluationPoints = new()
    {
        new MarketEvaluationPoint(Guid.NewGuid().ToString().Substring(5), Guid.NewGuid().ToString().Substring(10)),
    };

    public IReadOnlyCollection<MarketEvaluationPoint> MarketEvaluationPoints => _marketEvaluationPoints.AsReadOnly();

    public Task<MarketEvaluationPoint> GetByGsrnNumberAsync(string marketEvaluationPointId)
    {
        return Task.FromResult(_marketEvaluationPoints.First(x => x.GsrnNumber.Equals(marketEvaluationPointId, StringComparison.OrdinalIgnoreCase)));
    }

    public void Add(MarketEvaluationPoint marketEvaluationPoint)
    {
        _marketEvaluationPoints.Add(marketEvaluationPoint);
    }
}
