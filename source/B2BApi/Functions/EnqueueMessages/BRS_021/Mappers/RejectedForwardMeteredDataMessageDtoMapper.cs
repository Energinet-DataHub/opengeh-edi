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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021.Mappers;

internal static class RejectedForwardMeteredDataMessageDtoMapper
{
    internal static RejectedForwardMeteredDataMessageDto Map(
        Guid serviceBusMessageId,
        EnqueueActorMessagesActorV1 orchestrationStartedByActor,
        ForwardMeteredDataRejectedV1 rejectedData)
    {
        return new RejectedForwardMeteredDataMessageDto(
            eventId: EventId.From(serviceBusMessageId),
            externalId: new ExternalId(serviceBusMessageId),
            businessReason: BusinessReason.FromName(rejectedData.BusinessReason.Name),
            receiverNumber: ActorNumber.Create(orchestrationStartedByActor.ActorNumber),
            receiverRole: ActorRole.FromName(orchestrationStartedByActor.ActorRole.ToString()),
            documentReceiverRole: ActorRole.FromName(rejectedData.ForwardedForActorRole.Name),
            relatedToMessageId: MessageId.Create(rejectedData.OriginalActorMessageId),
            series: new RejectedForwardMeteredDataSeries(
                OriginalTransactionIdReference: TransactionId.From(rejectedData.OriginalTransactionId),
                TransactionId: TransactionId.New(),
                RejectReasons: rejectedData.ValidationErrors.Select(
                        validationError =>
                            new RejectReason(
                                ErrorCode: validationError.ErrorCode,
                                ErrorMessage: validationError.Message))
                    .ToList()));
    }
}
