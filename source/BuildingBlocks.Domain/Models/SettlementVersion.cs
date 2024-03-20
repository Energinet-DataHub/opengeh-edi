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

[Serializable]
public class SettlementVersion : EnumerationType
{
    // Must match the SettlementSeriesVersion names in Energinet.DataHub.Wholesale.Edi.Models.SettlementSeriesVersion
    public static readonly SettlementVersion FirstCorrection = new("FirstCorrection", "D01");
    public static readonly SettlementVersion SecondCorrection = new("SecondCorrection", "D02");
    public static readonly SettlementVersion ThirdCorrection = new("ThirdCorrection", "D03");

    // Below SettlementVersions are not used directly, but must be here for possible mapping
    public static readonly SettlementVersion FourthCorrection = new(nameof(FourthCorrection), "D04");
    public static readonly SettlementVersion FifthCorrection = new(nameof(FifthCorrection), "D05");
    public static readonly SettlementVersion SixthCorrection = new(nameof(SixthCorrection), "D06");
    public static readonly SettlementVersion SeventhCorrection = new(nameof(SeventhCorrection), "D07");
    public static readonly SettlementVersion EighthCorrection = new(nameof(EighthCorrection), "D08");
    public static readonly SettlementVersion NinthCorrection = new(nameof(NinthCorrection), "D09");
    public static readonly SettlementVersion TenthCorrection = new(nameof(TenthCorrection), "D10");

    [JsonConstructor]
    private SettlementVersion(string name, string code)
        : base(name)
    {
        Code = code;
    }

    public string Code { get; }

    public static SettlementVersion FromName(string name)
    {
        return GetAll<SettlementVersion>().FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException(
                   $"{name} is not a valid {typeof(SettlementVersion)} {nameof(name)}");
    }

    public static SettlementVersion FromCode(string code)
    {
        return GetAll<SettlementVersion>().FirstOrDefault(t => t.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException(
                   $"{code} is not a valid {typeof(SettlementVersion)} {nameof(code)}");
    }
}
