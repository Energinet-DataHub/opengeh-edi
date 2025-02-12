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
public class MeasurementUnit : DataHubType<MeasurementUnit>
{
    // Tariffs are measured in Kwh
    public static readonly MeasurementUnit KilowattHour = new(PMTypes.MeasurementUnit.KilowattHour.Name, "KWH");

    // Subscription and Fees are measured in pieces
    public static readonly MeasurementUnit Pieces = new(PMTypes.MeasurementUnit.Pieces.Name, "H87");

    [JsonConstructor]
    private MeasurementUnit(string name, string code)
        : base(name, code)
    {
    }

    public static MeasurementUnit? TryFromChargeType(ChargeType? chargeType)
    {
        if (chargeType is null) return null;

        var chargeTypeToMeasurementUnitMap = new Dictionary<ChargeType, MeasurementUnit>
        {
            { ChargeType.Tariff, KilowattHour },
            { ChargeType.Subscription, Pieces },
            { ChargeType.Fee, Pieces },
        };

        if (chargeTypeToMeasurementUnitMap.TryGetValue(chargeType, out var measurementUnit))
        {
            return measurementUnit;
        }

        throw new InvalidOperationException("Unknown charge type");
    }
}
