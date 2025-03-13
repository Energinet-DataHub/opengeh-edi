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

using System.Text.Json.Serialization;
using PMTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class Quality : DataHubType<Quality>
{
    public static readonly Quality NotAvailable = new(PMTypes.Quality.NotAvailable.Name, "A02");
    public static readonly Quality Estimated = new(PMTypes.Quality.Estimated.Name, "A03");
    public static readonly Quality Measured = new(PMTypes.Quality.AsProvided.Name, "A04");
    public static readonly Quality Incomplete = new(PMTypes.Quality.Incomplete.Name, "A05");
    public static readonly Quality Calculated = new(PMTypes.Quality.Calculated.Name, "A06");

    [JsonConstructor]
    private Quality(string name, string code)
        : base(name, code)
    {
    }

    public static string? TryGetNameFromEbixCode(string? code, string? fallbackValue)
    {
        return code switch
        {
            "36" => "36", // 36 is deprecated
            "56" => Estimated.Name,
            "E01" => Measured.Name,
            "D01" => Calculated.Name,
            _ => fallbackValue,
        };
    }
}
