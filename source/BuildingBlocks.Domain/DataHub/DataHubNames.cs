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

using System.Diagnostics.CodeAnalysis;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;

/// <summary>
/// These DataHub domain names are shared between the EDI and Wholesale subsystems
/// When updating these, you need to manually update the classes in the other subsystem
/// Files to manually keep in sync:
/// - EDI: BuildingBlocks.Domain/DataHub/DataHubNames.cs
/// - Wholesale: Edi/Edi/Contracts/DataHubNames.cs
/// In the future this should be shared through a NuGet package instead
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Keep names in a single file to easily share with Wholesale")]
public static class DataHubNames
{
    public static class Currency
    {
        public const string DanishCrowns = "DanishCrowns";
    }
}
