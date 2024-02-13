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
using System.Linq;
using System.Text.Json.Serialization;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public class SettlementVersion : EnumerationType
{
    public static readonly SettlementVersion FirstCorrection = new(1, nameof(FirstCorrection), "D01");
    public static readonly SettlementVersion SecondCorrection = new(2, nameof(SecondCorrection), "D02");
    public static readonly SettlementVersion ThirdCorrection = new(3, nameof(ThirdCorrection), "D03");

    // Below SettlementVersions are not used directly, but must be here for possible mapping
    public static readonly SettlementVersion FourthCorrection = new(4, nameof(FourthCorrection), "D04");
    public static readonly SettlementVersion FifthCorrection = new(5, nameof(FifthCorrection), "D05");
    public static readonly SettlementVersion SixthCorrection = new(6, nameof(SixthCorrection), "D06");
    public static readonly SettlementVersion SeventhCorrection = new(7, nameof(SeventhCorrection), "D07");
    public static readonly SettlementVersion EighthCorrection = new(8, nameof(EighthCorrection), "D08");
    public static readonly SettlementVersion NinthCorrection = new(9, nameof(NinthCorrection), "D09");
    public static readonly SettlementVersion TenthCorrection = new(10, nameof(TenthCorrection), "D10");

    [JsonConstructor]
    private SettlementVersion(int id, string name, string code)
        : base(id, name)
    {
        Code = code;
    }

    public string Code { get; }

    public static SettlementVersion FromName(string name)
    {
        var settlementVersion = GetAll<SettlementVersion>()
            .FirstOrDefault(type => type.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidCastException($"Could not parse name {name} to settlement version");

        return settlementVersion;
    }

    public static SettlementVersion FromCode(string code)
    {
        var settlementVersion = GetAll<SettlementVersion>()
            .FirstOrDefault(type => type.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidCastException($"Could not parse code {code} to settlement version");

        return settlementVersion;
    }
}
