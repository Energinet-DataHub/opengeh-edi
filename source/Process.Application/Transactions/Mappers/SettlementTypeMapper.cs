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

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;

public static class SettlementTypeMapper
{
    public static SettlementType? MapSettlementType(EnergyResultProducedV2.Types.TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
        {
            // There exist no corresponding SettlementType for these TimeSeriesTypes
            EnergyResultProducedV2.Types.TimeSeriesType.Production or
            EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerGa or
            EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerNeighboringGa or
            EnergyResultProducedV2.Types.TimeSeriesType.GridLoss or
            EnergyResultProducedV2.Types.TimeSeriesType.NegativeGridLoss or
            EnergyResultProducedV2.Types.TimeSeriesType.PositiveGridLoss or
            EnergyResultProducedV2.Types.TimeSeriesType.TotalConsumption or
            EnergyResultProducedV2.Types.TimeSeriesType.TempFlexConsumption or
            EnergyResultProducedV2.Types.TimeSeriesType.TempProduction => null,

            EnergyResultProducedV2.Types.TimeSeriesType.FlexConsumption => SettlementType.Flex,
            EnergyResultProducedV2.Types.TimeSeriesType.NonProfiledConsumption => SettlementType.NonProfiled,
            EnergyResultProducedV2.Types.TimeSeriesType.Unspecified => throw new InvalidOperationException("Could not map time series type"),
            _ => throw new ArgumentOutOfRangeException(nameof(timeSeriesType), timeSeriesType, "Unknown time series type from Wholesale"),
        };
    }
}
