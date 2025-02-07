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
public class SettlementMethod : DataHubType<SettlementMethod>
{
    // Customer with more than ~100.000 kwH per year
    public static readonly SettlementMethod NonProfiled = new(PMTypes.SettlementMethod.NonProfiled.Name, "E02");

    // Customer with less than ~100.000 kwH per year
    public static readonly SettlementMethod Flex = new(PMTypes.SettlementMethod.Flex.Name, "D01");

    [JsonConstructor]
    private SettlementMethod(string name, string code)
        : base(name, code)
    {
    }
}
