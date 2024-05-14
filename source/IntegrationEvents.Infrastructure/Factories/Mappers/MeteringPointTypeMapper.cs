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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;

public static class MeteringPointTypeMapper
{
    public static MeteringPointType Map(EnergyResultProducedV2.Types.TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
        {
            EnergyResultProducedV2.Types.TimeSeriesType.Production => MeteringPointType.Production,
            EnergyResultProducedV2.Types.TimeSeriesType.FlexConsumption => MeteringPointType.Consumption,
            EnergyResultProducedV2.Types.TimeSeriesType.NonProfiledConsumption => MeteringPointType.Consumption,
            EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerGa => MeteringPointType.Exchange,
            EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerNeighboringGa => MeteringPointType.Exchange,
            EnergyResultProducedV2.Types.TimeSeriesType.TotalConsumption => MeteringPointType.Consumption,
            EnergyResultProducedV2.Types.TimeSeriesType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
            _ => throw new ArgumentOutOfRangeException(nameof(timeSeriesType), timeSeriesType, "Unknown time series type unit from Wholesale"),
        };
    }

    public static MeteringPointType Map(AmountPerChargeResultProducedV1.Types.MeteringPointType meteringPointType)
    {
        return meteringPointType switch
        {
            AmountPerChargeResultProducedV1.Types.MeteringPointType.Production => MeteringPointType.Production,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.Consumption => MeteringPointType.Consumption,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.VeProduction => MeteringPointType.VeProduction,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.NetProduction => MeteringPointType.NetProduction,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.SupplyToGrid => MeteringPointType.SupplyToGrid,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.ConsumptionFromGrid => MeteringPointType.ConsumptionFromGrid,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.WholesaleServicesInformation => MeteringPointType.WholesaleServicesInformation,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.OwnProduction => MeteringPointType.OwnProduction,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.NetFromGrid => MeteringPointType.NetFromGrid,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.NetToGrid => MeteringPointType.NetToGrid,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.TotalConsumption => MeteringPointType.TotalConsumption,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.ElectricalHeating => MeteringPointType.ElectricalHeating,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.NetConsumption => MeteringPointType.NetConsumption,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.EffectSettlement => MeteringPointType.EffectSettlement,
            AmountPerChargeResultProducedV1.Types.MeteringPointType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
            _ => throw new ArgumentOutOfRangeException(nameof(meteringPointType), meteringPointType, "Unknown time series type unit from Wholesale"),
        };
    }
}
