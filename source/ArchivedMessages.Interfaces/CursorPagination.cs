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
/// The SortedCursorBasedPagination is navigating through a dataset by moving the cursor to a specific position in the sorted dataset.
/// </summary>
/// <param name="cursor"></param>
/// <param name="pageSize"></param>
/// <param name="navigationForward"></param>
/// <param name="fieldToSortBy"></param>
public class SortedCursorBasedPagination(
    PaginationCursor? cursor = null,
    int pageSize = 100,
    bool navigationForward = true,
    FieldToSortBy? fieldToSortBy = null)
{
    /// <summary>
    ///  The current position in the dataset.
    /// </summary>
    public PaginationCursor Cursor { get; } = cursor ?? new PaginationCursor(null, 0);

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; } = pageSize > 0 ? pageSize : throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be a positive number.");

    /// <summary>
    ///  A boolean indicating the direction of pagination.
    /// </summary>
    public bool NavigationForward { get; } = navigationForward;

    /// <summary>
    ///  A boolean indicating the direction of pagination.
    /// </summary>
    public FieldToSortBy FieldToSortBy { get; } = fieldToSortBy ?? FieldToSortBy.CreatedAt;
}

/// <summary>
/// The current last known position in the dataset. Used to navigate through the dataset.
/// Current displayed dataset:
/// -- Looking ahead
/// [0]
/// [1]
/// [2]
/// [3]
/// [4] - Cursor
/// -- Looking backwards
/// [9]
/// [8]
/// [7]
/// [6]
/// [5] - Cursor
/// </summary>
/// <param name="SortedField">If dataset should be short, we need to point at the current value of the field being sorted on.</param>
/// <param name="RecordId"></param>
public record PaginationCursor(string? SortedField = null, long RecordId = 0);
