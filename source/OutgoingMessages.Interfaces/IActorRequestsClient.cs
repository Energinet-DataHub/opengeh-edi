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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces;

public interface IActorRequestsClient
{
    /// <summary>
    /// Enqueues aggregated measure data, if data is found.
    /// </summary>
    /// <param name="businessReason"></param>
    /// <param name="aggregatedTimeSeriesQueryParameters"></param>
    public Task EnqueueAggregatedMeasureDataAsync(string businessReason, AggregatedTimeSeriesQueryParameters aggregatedTimeSeriesQueryParameters);

    public Task EnqueueWholesaleServicesAsync(
        WholesaleServicesQueryParameters wholesaleServicesQueryParameters,
        ActorNumber requestedByActorNumber,
        ActorRole requestedByActorRole,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        Guid orchestrationInstanceId,
        EventId eventId,
        MessageId originalMessageId,
        TransactionId originalTransactionId,
        CancellationToken cancellationToken);

    public Task EnqueueRejectAggregatedMeasureDataRequestAsync(
        RejectedEnergyResultMessageDto rejectedEnergyResultMessageDto,
        CancellationToken cancellationToken);
}
