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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces;

public interface IActorRequestsClient
{
    /// <summary>
    /// Enqueues aggregated measure data, if data is found.
    /// </summary>
    /// <param name="orchestrationInstanceId"></param>
    /// <param name="originalTransactionId"></param>
    /// <param name="originalMessageId"></param>
    /// <param name="requestedForActorNumber"></param>
    /// <param name="requestedForActorRole"></param>
    /// <param name="businessReason"></param>
    /// <param name="meteringPointType"></param>
    /// <param name="settlementMethod"></param>
    /// <param name="settlementVersion"></param>
    /// <param name="aggregatedTimeSeriesQueryParameters"></param>
    public Task EnqueueAggregatedMeasureDataAsync(
        string orchestrationInstanceId,
        string originalTransactionId,
        string originalMessageId,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        BusinessReason businessReason,
        MeteringPointType? meteringPointType,
        SettlementMethod? settlementMethod,
        SettlementVersion? settlementVersion,
        AggregatedTimeSeriesQueryParameters aggregatedTimeSeriesQueryParameters);
}
