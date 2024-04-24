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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
/// The file category ("archived", "outgoing" etc.). In Azure Blob Storage this references where which container the blob is stored inside.
/// </summary>
public record FileStorageCategory
{
    private const string ArchivedMessageCategory = "archived";
    private const string OutgoingMessageCategory = "outgoing";

    private FileStorageCategory(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static FileStorageCategory ArchivedMessage() => new(ArchivedMessageCategory);

    public static FileStorageCategory OutgoingMessage() => new(OutgoingMessageCategory);
}
