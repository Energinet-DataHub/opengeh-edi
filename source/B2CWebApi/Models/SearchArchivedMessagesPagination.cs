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

namespace Energinet.DataHub.EDI.B2CWebApi.Models;

/// <summary>
/// Pagination when searching for archived messages that supports sorting on a specific field.
/// </summary>
/// <param name="Cursor"></param>
/// <param name="PageSize"></param>
/// <param name="NavigationForward"></param>
/// <param name="SortBy"></param>
/// <param name="DirectionToSortBy"></param>
[Serializable]
public record SearchArchivedMessagesPagination(
    SearchArchivedMessagesCursor? Cursor,
    FieldToSortBy? SortBy = null,
    DirectionToSortBy? DirectionToSortBy = null,
    int PageSize = 100,
    bool NavigationForward = true);
