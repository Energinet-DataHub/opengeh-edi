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

namespace Messaging.Domain.MasterData.MarketEvaluationPoints;

/// <summary>
/// Repository of energy suppliers for accounting points
/// </summary>
public interface IMarketEvaluationPointRepository
{
    /// <summary>
    /// Find the energy supplier for a give accounting point
    /// </summary>
    /// <param name="marketEvaluationPointNumber">GSRN-number of accounting point</param>
    Task<MarketEvaluationPoint?> GetByNumberAsync(string marketEvaluationPointNumber);

    /// <summary>
    /// Add energy supplier to repository
    /// </summary>
    /// <param name="marketEvaluationPoint"></param>
    void Add(MarketEvaluationPoint marketEvaluationPoint);
}
