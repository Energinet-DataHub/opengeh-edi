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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Domain.Actors;
using Domain.Transactions.Aggregations;

namespace Application.Transactions.Aggregations;

/// <summary>
/// Retrieves stored aggregation results
/// </summary>
public interface IAggregationResults
{
    /// <summary>
    /// Fetch a result by id
    /// </summary>
    /// <param name="resultId"></param>
    /// <param name="gridArea"></param>
    /// <param name="period"></param>
    Task<AggregationResult> GetResultAsync(Guid resultId, string gridArea, Domain.Transactions.Aggregations.Period period);

    /// <summary>
    /// Fetch a list of energy supplier numbers for which an aggregation result of hourly consumption is available
    /// </summary>
    /// <param name="resultId"></param>
    /// <param name="gridArea"></param>
    Task<ReadOnlyCollection<ActorNumber>> EnergySuppliersWithHourlyConsumptionResultAsync(Guid resultId, string gridArea);

    /// <summary>
    /// Fetch aggregation result of "hourly consumption"
    /// </summary>
    /// <param name="resultId"></param>
    /// <param name="gridArea"></param>
    /// <param name="energySupplierNumber"></param>
    /// <param name="period"></param>
    Task<AggregationResult> HourlyConsumptionForAsync(Guid resultId, string gridArea, ActorNumber energySupplierNumber, Domain.Transactions.Aggregations.Period period);
}
