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

public record FieldToSortBy(string Identifier)
{
    public static readonly FieldToSortBy MessageId = new("MessageId");
    public static readonly FieldToSortBy DocumentType = new("DocumentType");
    public static readonly FieldToSortBy Sender = new("Sender");
    public static readonly FieldToSortBy Receiver = new("Receiver");
    public static readonly FieldToSortBy CreatedAt = new("CreatedAt");
}

public record DirectionSortBy(string Identifier)
{
    public static readonly DirectionSortBy Ascending = new("ASC");
    public static readonly DirectionSortBy Descending = new("DESC");
}
