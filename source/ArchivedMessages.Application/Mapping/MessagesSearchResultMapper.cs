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

using System.Collections.ObjectModel;
using Energinet.DataHub.EDI.ArchivedMessages.Domain.Models;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.ArchivedMessages.Application.Mapping;

internal static class MessagesSearchResultMapper
{
    internal static MessageSearchResultDto Map(MessageSearchResult messageSearchResult)
    {
        return new MessageSearchResultDto(SetMessageInfoCollection(messageSearchResult.Messages), messageSearchResult.TotalAmountOfMessages);
    }

    private static ReadOnlyCollection<MessageInfoDto> SetMessageInfoCollection(IReadOnlyCollection<MessageInfo> collection)
    {
        return collection.Select(mi => new MessageInfoDto(
            RecordId: mi.RecordId,
            Id: mi.Id,
            MessageId: mi.MessageId,
            DocumentType: mi.DocumentType,
            SenderNumber: mi.SenderNumber,
            SenderRoleCode: mi.SenderRoleCode,
            ReceiverNumber: mi.ReceiverNumber,
            ReceiverRoleCode: mi.ReceiverRoleCode,
            CreatedAt: mi.CreatedAt,
            BusinessReason: mi.BusinessReason))
        .ToList()
        .AsReadOnly();
    }
}
