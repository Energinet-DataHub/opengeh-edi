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

using Energinet.DataHub.EDI.Domain.Common;

namespace Energinet.DataHub.EDI.Domain.Transactions.Aggregations;

public class SettlementVersion : EnumerationType
{
    public static readonly SettlementVersion FirstCorrection = new(0, nameof(FirstCorrection), "D01");
    public static readonly SettlementVersion SecondCorrection = new(0, nameof(SecondCorrection), "D02");
    public static readonly SettlementVersion ThirdCorrection = new(0, nameof(ThirdCorrection), "D03");
    public static readonly SettlementVersion FourthCorrection = new(0, nameof(FourthCorrection), "D04");
    public static readonly SettlementVersion FifthCorrection = new(0, nameof(FifthCorrection), "D05");
    public static readonly SettlementVersion SixthCorrection = new(0, nameof(SixthCorrection), "D06");
    public static readonly SettlementVersion SeventhCorrection = new(0, nameof(SeventhCorrection), "D07");
    public static readonly SettlementVersion EighthCorrection = new(0, nameof(EighthCorrection), "D08");
    public static readonly SettlementVersion NinthCorrection = new(0, nameof(NinthCorrection), "D09");
    public static readonly SettlementVersion TenthCorrection = new(0, nameof(TenthCorrection), "D10");

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
