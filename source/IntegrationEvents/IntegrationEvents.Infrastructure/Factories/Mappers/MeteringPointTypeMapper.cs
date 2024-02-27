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
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Exceptions;
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
            EnergyResultProducedV2.Types.TimeSeriesType.GridLoss => throw new NotSupportedTimeSeriesTypeException("GridLoss is not a supported TimeSeriesType"),
            EnergyResultProducedV2.Types.TimeSeriesType.TempProduction => throw new NotSupportedTimeSeriesTypeException("TempProduction is not a supported TimeSeriesType"),
            EnergyResultProducedV2.Types.TimeSeriesType.NegativeGridLoss => throw new NotSupportedTimeSeriesTypeException("NegativeGridLoss is not a supported TimeSeriesType"),
            EnergyResultProducedV2.Types.TimeSeriesType.PositiveGridLoss => throw new NotSupportedTimeSeriesTypeException("PositiveGridLoss is not a supported TimeSeriesType"),
            EnergyResultProducedV2.Types.TimeSeriesType.TempFlexConsumption => throw new NotSupportedTimeSeriesTypeException("TempFlexConsumption is not a supported TimeSeriesType"),
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
            AmountPerChargeResultProducedV1.Types.MeteringPointType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
            _ => throw new ArgumentOutOfRangeException(nameof(meteringPointType), meteringPointType, "Unknown time series type unit from Wholesale"),
        };
    }
}
