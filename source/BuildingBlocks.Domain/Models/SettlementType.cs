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
using System.Text.Json.Serialization;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class SettlementType : EnumerationTypeWithCode<SettlementType>
{
    // Must match the SettlementMethod names in Energinet.DataHub.Wholesale.Edi.Models.SettlementMethod

    // Customer with more than ~100.000 kwH per year
    public static readonly SettlementType NonProfiled = new(nameof(NonProfiled), "E02");

    // Customer with less than ~100.000 kwH per year
    public static readonly SettlementType Flex = new(nameof(Flex), "D01");

    [JsonConstructor]
    private SettlementType(string name, string code)
        : base(name, code)
    {
    }
}
