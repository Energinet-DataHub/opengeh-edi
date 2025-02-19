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

using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults;

public static class MeteringPointTypeMapper
{
    public static BuildingBlocks.Domain.Models.MeteringPointType? FromDeltaTableValue(string? meteringPointType)
    {
        if (meteringPointType == null)
            return null;

        Dictionary<string, BuildingBlocks.Domain.Models.MeteringPointType> meteringPointTypeMap = new()
        {
            { DeltaTableMeteringPointType.Consumption, BuildingBlocks.Domain.Models.MeteringPointType.Consumption },
            { DeltaTableMeteringPointType.Production, BuildingBlocks.Domain.Models.MeteringPointType.Production },
            { DeltaTableMeteringPointType.Exchange, BuildingBlocks.Domain.Models.MeteringPointType.Exchange },
            { DeltaTableMeteringPointType.VeProduction, BuildingBlocks.Domain.Models.MeteringPointType.VeProduction },
            { DeltaTableMeteringPointType.NetProduction, BuildingBlocks.Domain.Models.MeteringPointType.NetProduction },
            { DeltaTableMeteringPointType.SupplyToGrid, BuildingBlocks.Domain.Models.MeteringPointType.SupplyToGrid },
            { DeltaTableMeteringPointType.ConsumptionFromGrid, BuildingBlocks.Domain.Models.MeteringPointType.ConsumptionFromGrid },
            { DeltaTableMeteringPointType.WholesaleServicesInformation, BuildingBlocks.Domain.Models.MeteringPointType.WholesaleServicesInformation },
            { DeltaTableMeteringPointType.OwnProduction, BuildingBlocks.Domain.Models.MeteringPointType.OwnProduction },
            { DeltaTableMeteringPointType.NetFromGrid, BuildingBlocks.Domain.Models.MeteringPointType.NetFromGrid },
            { DeltaTableMeteringPointType.NetToGrid, BuildingBlocks.Domain.Models.MeteringPointType.NetToGrid },
            { DeltaTableMeteringPointType.TotalConsumption, BuildingBlocks.Domain.Models.MeteringPointType.TotalConsumption },
            { DeltaTableMeteringPointType.ElectricalHeating, BuildingBlocks.Domain.Models.MeteringPointType.ElectricalHeating },
            { DeltaTableMeteringPointType.NetConsumption, BuildingBlocks.Domain.Models.MeteringPointType.NetConsumption },
            { DeltaTableMeteringPointType.CapacitySettlement, BuildingBlocks.Domain.Models.MeteringPointType.CapacitySettlement },
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
}
