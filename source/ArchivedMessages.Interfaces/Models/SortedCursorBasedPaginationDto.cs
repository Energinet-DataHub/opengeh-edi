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

namespace Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;

public sealed record SortedCursorBasedPaginationDto(
    SortingCursorDto? Cursor = null,
    int PageSize = 100,
    bool NavigationForward = true,
    FieldToSortByDto? FieldToSortBy = null,
    DirectionToSortByDto? DirectionToSortBy = null)
{
    /// <summary>
    ///  The current position in the dataset.
    /// </summary>
    public SortingCursorDto Cursor { get; } = Cursor ?? new SortingCursorDto();

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; } = PageSize > 0 ? PageSize : throw new ArgumentOutOfRangeException(nameof(PageSize), "Page size must be a positive number.");

    /// <summary>
    ///  A boolean indicating the direction of pagination.
    /// </summary>
    public bool NavigationForward { get; } = NavigationForward;

    /// <summary>
    ///  The field to sort by.
    /// </summary>
    public FieldToSortByDto SortBy { get; } = FieldToSortBy ?? FieldToSortByDto.CreatedAt;

    /// <summary>
    /// The direction to sort by.
    /// </summary>
    public DirectionToSortByDto SortByDirection { get; } = DirectionToSortBy ?? DirectionToSortByDto.Descending;
}

public sealed record SortingCursorDto(string? SortedFieldValue = null, long RecordId = 0);
