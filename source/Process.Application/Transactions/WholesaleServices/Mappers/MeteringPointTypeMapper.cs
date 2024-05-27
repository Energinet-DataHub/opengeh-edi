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
using Energinet.DataHub.Edi.Responses;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Mappers;

public static class MeteringPointTypeMapper
{
    public static MeteringPointType Map(WholesaleServicesRequestSeries.Types.MeteringPointType meteringPointType)
    {
        return meteringPointType switch
        {
            WholesaleServicesRequestSeries.Types.MeteringPointType.Production => MeteringPointType.Production,
            WholesaleServicesRequestSeries.Types.MeteringPointType.Consumption => MeteringPointType.Consumption,
            WholesaleServicesRequestSeries.Types.MeteringPointType.VeProduction => MeteringPointType.VeProduction,
            WholesaleServicesRequestSeries.Types.MeteringPointType.NetProduction => MeteringPointType.NetProduction,
            WholesaleServicesRequestSeries.Types.MeteringPointType.SupplyToGrid => MeteringPointType.SupplyToGrid,
            WholesaleServicesRequestSeries.Types.MeteringPointType.ConsumptionFromGrid => MeteringPointType.ConsumptionFromGrid,
            WholesaleServicesRequestSeries.Types.MeteringPointType.WholesaleServicesInformation => MeteringPointType.WholesaleServicesInformation,
            WholesaleServicesRequestSeries.Types.MeteringPointType.OwnProduction => MeteringPointType.OwnProduction,
            WholesaleServicesRequestSeries.Types.MeteringPointType.NetFromGrid => MeteringPointType.NetFromGrid,
            WholesaleServicesRequestSeries.Types.MeteringPointType.NetToGrid => MeteringPointType.NetToGrid,
            WholesaleServicesRequestSeries.Types.MeteringPointType.TotalConsumption => MeteringPointType.TotalConsumption,
            WholesaleServicesRequestSeries.Types.MeteringPointType.ElectricalHeating => MeteringPointType.ElectricalHeating,
            WholesaleServicesRequestSeries.Types.MeteringPointType.NetConsumption => MeteringPointType.NetConsumption,
            WholesaleServicesRequestSeries.Types.MeteringPointType.EffectSettlement => MeteringPointType.EffectSettlement,
            WholesaleServicesRequestSeries.Types.MeteringPointType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
            _ => throw new ArgumentOutOfRangeException(nameof(meteringPointType), meteringPointType, "Unknown metering point type"),
        };
    }
}
