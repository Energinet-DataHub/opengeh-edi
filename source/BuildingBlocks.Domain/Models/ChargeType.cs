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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class ChargeType : EnumerationType
{
    public static readonly ChargeType Subscription = new(nameof(Subscription), "D01");
    public static readonly ChargeType Fee = new(nameof(Fee), "D02");
    public static readonly ChargeType Tariff = new(nameof(Tariff), "D03");

    public ChargeType(string name, string code)
        : base(name)
    {
        Code = code;
    }

    public string Code { get; }

    public static ChargeType FromCode(string code)
    {
        return GetAll<ChargeType>().FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{code} is not a valid {typeof(ChargeType)} code");
    }

    public static ChargeType FromName(string name)
    {
        return GetAll<ChargeType>().FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{name} is not a valid {typeof(ChargeType)} {nameof(name)}");
    }

    public static ChargeType? TryFromCode(string code)
    {
        return GetAll<ChargeType>().FirstOrDefault(r => r.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}
