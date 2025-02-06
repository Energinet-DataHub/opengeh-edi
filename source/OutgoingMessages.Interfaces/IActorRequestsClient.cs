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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces;

public interface IActorRequestsClient
{
    public Task<int> EnqueueAggregatedMeasureDataAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        MessageId originalMessageId,
        TransactionId originalTransactionId,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        ActorNumber requestedByActorNumber,
        ActorRole requestedByActorRole,
        BusinessReason businessReason,
        MeteringPointType? meteringPointType,
        SettlementMethod? settlementMethod,
        SettlementVersion? settlementVersion,
        AggregatedTimeSeriesQueryParameters aggregatedTimeSeriesQueryParameters,
        CancellationToken cancellationToken);

    public Task<int> EnqueueWholesaleServicesAsync(
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

    public Task EnqueueRejectWholesaleServicesRequestAsync(
        RejectedWholesaleServicesMessageDto enqueueRejectedMessageDto,
        CancellationToken cancellationToken);

    public Task EnqueueRejectAggregatedMeasureDataRequestWithNoDataAsync(
        Guid orchestrationInstanceId,
        MessageId originalMessageId,
        EventId eventId,
        TransactionId originalTransactionId,
        ActorNumber requestedByActorNumber,
        ActorRole requestedByActorRole,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        BusinessReason businessReason,
        AggregatedTimeSeriesQueryParameters aggregatedTimeSeriesQueryParameters,
        CancellationToken cancellationToken);

    public Task EnqueueRejectWholesaleServicesRequestWithNoDataAsync(
        WholesaleServicesQueryParameters queryParameters,
        ActorNumber requestedByActorNumber,
        ActorRole requestedByActorRole,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        Guid orchestrationInstanceId,
        EventId eventId,
        MessageId originalMessageId,
        TransactionId originalTransactionId,
        BusinessReason businessReason,
        CancellationToken cancellationToken);
}
