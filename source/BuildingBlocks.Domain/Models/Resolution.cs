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

using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class Resolution : EnumerationType
{
    public static readonly Resolution QuarterHourly = new(nameof(QuarterHourly), "PT15M");
    public static readonly Resolution Hourly = new(nameof(Hourly), "PT1H");
    public static readonly Resolution Daily = new(nameof(Daily), "P1D");
    public static readonly Resolution Monthly = new(nameof(Monthly), "P1M");

    [JsonConstructor]
    private Resolution(string name, string code)
        : base(name)
    {
        Code = code;
    }

    public string Code { get; }

    public static Resolution FromName(string name)
    {
        return GetAll<Resolution>().FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{name} is not a valid {typeof(Resolution)} {nameof(name)}");
    }

    public static Resolution FromCode(string code)
    {
        return GetAll<Resolution>().FirstOrDefault(r => r.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{code} is not a valid {typeof(Resolution)} {nameof(code)}");
    }
}
