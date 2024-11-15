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

namespace Energinet.DataHub.EDI.ArchivedMessages.Domain.Models;

/// <summary>
/// The SortedCursorBasedPagination is navigating forward or backwards through a dataset
/// by moving the cursor to a specific position in the sorted dataset.
/// </summary>
public class SortedCursorBasedPagination(
    SortingCursor? cursor = null,
    int pageSize = 100,
    bool navigationForward = true,
    FieldToSortBy? fieldToSortBy = null,
    DirectionToSortBy? directionToSortBy = null)
{
    /// <summary>
    ///  The current position in the dataset.
    /// </summary>
    public SortingCursor Cursor { get; } = cursor ?? new SortingCursor();

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; } = pageSize > 0 ? pageSize : throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be a positive number.");

    /// <summary>
    ///  A boolean indicating the direction of pagination.
    /// </summary>
    public bool NavigationForward { get; } = navigationForward;

    /// <summary>
    ///  The field to sort by.
    /// </summary>
    public FieldToSortBy FieldToSortBy { get; } = fieldToSortBy ?? FieldToSortBy.CreatedAt;

    /// <summary>
    /// The direction to sort by.
    /// </summary>
    public DirectionToSortBy DirectionToSortBy { get; } = directionToSortBy ?? DirectionToSortBy.Descending;
}

/// <summary>
/// Pagination cursor for the search of archived messages.
/// Pointing to the field value to sort by and the record id.
/// When navigating forward, the cursor points to the last record of the current page.
/// and when navigating backward, the cursor points to the first record of the current page.
/// </summary>
/// <param name="SortedFieldValue">If dataset should be sorted, we need to point at the current value of the field being sorted on.</param>
/// <param name="RecordId"></param>
public record SortingCursor(string? SortedFieldValue = null, long RecordId = 0);
