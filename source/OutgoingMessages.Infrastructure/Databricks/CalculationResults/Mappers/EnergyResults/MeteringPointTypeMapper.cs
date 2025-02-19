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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;

public static class MeteringPointTypeMapper
{
    public static MeteringPointType FromDeltaTableValue(string meteringPointType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meteringPointType);

        Dictionary<string, MeteringPointType> meteringPointTypeMap = new()
        {
            { DeltaTableMeteringPointType.Consumption, MeteringPointType.Consumption },
            { DeltaTableMeteringPointType.Production, MeteringPointType.Production },
            { DeltaTableMeteringPointType.Exchange, MeteringPointType.Exchange },
        };

        if (!meteringPointTypeMap.TryGetValue(meteringPointType, out var value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(meteringPointType),
                actualValue: meteringPointType,
                "Value does not contain a valid string representation of a metering point type.");
        }

        return value;
    }

    public static string? ToDeltaTableValue(MeteringPointType? meteringPointType)
    {
        if (meteringPointType is null) return null;

        Dictionary<string, string> meteringPointTypeMap = new()
        {
            { MeteringPointType.Consumption.Name, DeltaTableMeteringPointType.Consumption },
            { MeteringPointType.Production.Name, DeltaTableMeteringPointType.Production },
            { MeteringPointType.Exchange.Name, DeltaTableMeteringPointType.Exchange },
        };

        if (!meteringPointTypeMap.TryGetValue(meteringPointType.Name, out var value))
        {
            throw new ArgumentOutOfRangeException(nameof(meteringPointType), meteringPointType, null);
        }

        return value;
    }
}
