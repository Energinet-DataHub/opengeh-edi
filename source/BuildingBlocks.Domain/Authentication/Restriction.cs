﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;

public class Restriction : EnumerationType
{
    /// <summary>
    /// No restrictions can see all data.
    /// </summary>
    public static readonly Restriction None = new(nameof(None));

    /// <summary>
    /// Restricted to only see own data.
    /// </summary>
    public static readonly Restriction Owned = new(nameof(Owned));

    private Restriction(string name)
        : base(name)
    {
    }
}
