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

using Energinet.DataHub.EDI.ArchivedMessages.Domain.Models;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.ArchivedMessages.Application.Mapping;

internal static class GetMessagesQueryMapper
{
    internal static GetMessagesQuery Map(GetMessagesQueryDto dto)
    {
        return new GetMessagesQuery(
            Pagination: SetSortedCursorBasedPagination(dto.Pagination),
            CreationPeriod: SetMessageCreationPeriod(dto.CreationPeriod),
            MessageId: dto.MessageId,
            SenderNumber: dto.SenderNumber,
            SenderRoleCode: dto.SenderRoleCode,
            ReceiverNumber: dto.ReceiverNumber,
            ReceiverRoleCode: dto.ReceiverRoleCode,
            DocumentTypes: dto.DocumentTypes,
            BusinessReasons: dto.BusinessReasons,
            IncludeRelatedMessages: dto.IncludeRelatedMessages);
    }

    private static SortedCursorBasedPagination SetSortedCursorBasedPagination(SortedCursorBasedPaginationDto dto)
    {
        return new SortedCursorBasedPagination(
                cursor: SetSortingCursor(dto.Cursor),
                pageSize: dto.PageSize,
                navigationForward: dto.NavigationForward,
                fieldToSortBy: SetFieldToSortBy(dto.FieldToSortBy),
                sortByDirection: SetDirectionToSortBy(dto.DirectionToSortBy));
    }

    private static MessageCreationPeriod? SetMessageCreationPeriod(MessageCreationPeriodDto? messageCreationPeriod)
    {
        return messageCreationPeriod is not null
            ? new MessageCreationPeriod(messageCreationPeriod.DateToSearchFrom, messageCreationPeriod.DateToSearchTo)
            : null;
    }

    private static SortingCursor? SetSortingCursor(SortingCursorDto? sortingCursor)
    {
        return sortingCursor is not null
            ? new SortingCursor(sortingCursor.SortedFieldValue, sortingCursor.RecordId)
            : null;
    }

    private static FieldToSortBy? SetFieldToSortBy(FieldToSortByDto? fieldToSortBy)
    {
        return fieldToSortBy is not null
            ? new FieldToSortBy(fieldToSortBy.Value.Identifier)
            : null;
    }

    private static DirectionToSortBy? SetDirectionToSortBy(DirectionToSortByDto? directionToSortBy)
    {
        return directionToSortBy is not null
            ? new DirectionToSortBy(directionToSortBy.Value.Identifier)
            : null;
    }
}
