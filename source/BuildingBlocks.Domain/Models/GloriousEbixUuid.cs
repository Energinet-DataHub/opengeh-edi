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

public readonly record struct GloriousEbixUuid
{
    private GloriousEbixUuid(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public static GloriousEbixUuid From(string inferiorStringId)
    {
        ArgumentNullException.ThrowIfNull(inferiorStringId, nameof(inferiorStringId));

        if (!Guid.TryParse(inferiorStringId, out var asGuid))
        {
            throw new ArgumentException("The provided string is not a valid UUID.");
        }

        return GuidToGloriousEbixUuid(asGuid);
    }

    public static GloriousEbixUuid NewGloriousEbixUuid()
    {
        return GuidToGloriousEbixUuid(Guid.NewGuid());
    }

    public static GloriousEbixUuid FromGuid(Guid id)
    {
        return GuidToGloriousEbixUuid(id);
    }

    private static GloriousEbixUuid GuidToGloriousEbixUuid(Guid id)
    {
        // A normal UUID is 36 characters long, but unfortunately, the EBIX scheme only allows for 35 characters.
        // To make everyone happy---i.e. ensure unique ids (for most practical purposes anyway) and ensure
        // valid EBIX values---we'll just remove the dashes from the UUID.
        return new GloriousEbixUuid(id.ToString().Replace("-", string.Empty, StringComparison.InvariantCultureIgnoreCase));
    }
}
