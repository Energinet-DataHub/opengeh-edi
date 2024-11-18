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

using Energinet.DataHub.EDI.ArchivedMessages.Domain.Exceptions;
using Energinet.DataHub.EDI.ArchivedMessages.Domain.Models;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Validation;

namespace Energinet.DataHub.EDI.ArchivedMessages.Application.Mapping;

public static class ArchivedMessageMapper
{
    public static ArchivedMessage Map(ArchivedMessageDto dto)
    {
        return !EnumCompatibilityChecker.AreEnumsCompatible<ArchivedMessageType, ArchivedMessageTypeDto>()
            ? throw new InvalidEnumMappingException($"Enum of type {nameof(ArchivedMessageType)} cannot be mapped to type {nameof(ArchivedMessageTypeDto)}.")
            : new ArchivedMessage(
            id: new ArchivedMessageId(dto.Id.Value),
            messageId: dto.MessageId,
            eventIds: dto.EventIds,
            documentType: dto.DocumentType,
            senderNumber: dto.SenderNumber,
            senderRole: dto.SenderRole,
            receiverNumber: dto.ReceiverNumber,
            receiverRole: dto.ReceiverRole,
            createdAt: dto.CreatedAt,
            businessReason: dto.BusinessReason,
            archivedMessageType: (ArchivedMessageType)dto.ArchivedMessageType,
            archivedMessageStream: new ArchivedMessageStream(dto.ArchivedMessageStream.Stream),
            relatedToMessageId: dto.RelatedToMessageId);
    }
}
