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
public class MeasurementUnit : EnumerationType
{
    // Tariffs are measured in Kwh
    public static readonly MeasurementUnit Kwh = new(nameof(Kwh), "KWH");

    // Subscription and Fees are measured in pieces
    public static readonly MeasurementUnit Pieces = new(nameof(Pieces), "H87");

    [JsonConstructor]
    private MeasurementUnit(string name, string code)
        : base(name)
    {
        Code = code;
    }

    public string Code { get; }

    public static MeasurementUnit FromName(string name)
    {
        return GetAll<MeasurementUnit>().First(type =>
                   type.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException(
                   $"{name} is not a valid {typeof(MeasurementUnit)} {nameof(name)}");
    }

    public static MeasurementUnit FromCode(string code)
    {
        return GetAll<MeasurementUnit>()
                   .First(
                       type =>
                           type.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException(
                   $"{code} is not a valid {typeof(MeasurementUnit)} {nameof(code)}");
    }
}
