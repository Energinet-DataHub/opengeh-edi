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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models;

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

        if (!Guid.TryParse(inferiorStringId, out _))
        {
            throw new ArgumentException("The provided string is not a valid UUID.");
        }

        return new GloriousEbixUuid(inferiorStringId.Replace("-", string.Empty, StringComparison.InvariantCultureIgnoreCase));
    }
}
