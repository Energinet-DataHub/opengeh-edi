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

namespace Energinet.DataHub.EDI.ArchivedMessages.Interfaces;

/// <summary>
/// Represents a query options for retrieving messages.
/// Including the pagination for the specific query.
/// </summary>
/// <param name="Pagination"></param>
/// <param name="CreationPeriod"></param>
/// <param name="MessageId"></param>
/// <param name="SenderNumber"></param>
/// <param name="ReceiverNumber"></param>
/// <param name="DocumentTypes"></param>
/// <param name="BusinessReasons"></param>
/// <param name="IncludeRelatedMessages"></param>
public sealed record GetMessagesQuery(
    SortedCursorBasedPagination Pagination,
    MessageCreationPeriod? CreationPeriod = null,
    string? MessageId = null,
    string? SenderNumber = null,
    string? ReceiverNumber = null,
    IReadOnlyCollection<string>? DocumentTypes = null,
    IReadOnlyCollection<string>? BusinessReasons = null,
    bool IncludeRelatedMessages = false);
