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

using NodaTime;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
/// Represents the needed metadata to where a file is stored.
/// In Azure Blob Storage the category would represent the container,
/// and the path is where the blob is stored inside the container.
/// </summary>
public record FileStorageReference
{
    public FileStorageReference(FileStorageCategory category, string path)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentException.ThrowIfNullOrEmpty(path);

        Category = category;
        Path = path;
    }

    /// <summary>
    /// The path the file is stored in. In Azure Blob Storage this references where a blob is stored inside a container.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The file's category. In Azure Blob Storage this references where which container the blob is stored inside.
    /// </summary>
    public FileStorageCategory Category { get; }

    /// <summary>
    /// Create a new FileStorageReference, with a path like "{actorNumber}/{year:0000}/{month:00}/{day:00}/{id}"
    /// </summary>
    /// <param name="category">The file category, some examples could be "archived", "outgoing" etc.DataLakeFileStorageClient uses this to determine which container the file should be stored inside.</param>
    /// <param name="actorNumber">The actor number related to the file, used to determine the path the file is stored in.</param>
    /// <param name="timeStamp">The timestamp related to the file, used to determine the path the file is stored in.</param>
    /// <param name="id">A guid representing the blob, used to determine the path the file is stored in. Is converted to a only contain letters and numbers, to ensure maximum compatibility with file systems</param>
    public static FileStorageReference Create(FileStorageCategory category, string actorNumber, Instant timeStamp, Guid id)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);

        var dateTimeUtc = timeStamp.ToDateTimeUtc();

        var uniqueFileId = id.ToString("N"); // Converted to only contain digits (letters and numbers), to ensure maximum compatibility with file systems.

        var reference = $"{actorNumber}/{dateTimeUtc.Year:0000}/{dateTimeUtc.Month:00}/{dateTimeUtc.Day:00}/{uniqueFileId}";

        return new FileStorageReference(category, reference);
    }
}

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
