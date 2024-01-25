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

using System;
using NodaTime;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public record FileStorageReference
{
    public FileStorageReference(string category, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(category);
        ArgumentException.ThrowIfNullOrEmpty(path);

        Category = category;
        Value = path;
    }

    public string Value { get; }

    public string Category { get; }

    public static FileStorageReference Create(string category, string actorNumber, Instant timeStamp, string documentId)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);

        var dateTimeUtc = timeStamp.ToDateTimeUtc();

        var reference = $"{actorNumber}/{dateTimeUtc.Year:0000}/{dateTimeUtc.Month:00}/{dateTimeUtc.Day:00}/{documentId}";

        return new FileStorageReference(category, reference);
    }
}
