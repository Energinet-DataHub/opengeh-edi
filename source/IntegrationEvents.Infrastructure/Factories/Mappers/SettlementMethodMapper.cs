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

public static class SettlementMethodMapper
{
    public static SettlementMethod? Map(EnergyResultProducedV2.Types.TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
        {
            // There exist no corresponding SettlementMethod for these TimeSeriesTypes
            EnergyResultProducedV2.Types.TimeSeriesType.Production or
            EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerGa or
            EnergyResultProducedV2.Types.TimeSeriesType.TotalConsumption => null,
            EnergyResultProducedV2.Types.TimeSeriesType.FlexConsumption => SettlementMethod.Flex,
            EnergyResultProducedV2.Types.TimeSeriesType.NonProfiledConsumption => SettlementMethod.NonProfiled,
            EnergyResultProducedV2.Types.TimeSeriesType.Unspecified => throw new InvalidOperationException("Could not map time series type"),
            _ => throw new ArgumentOutOfRangeException(nameof(timeSeriesType), timeSeriesType, "Unknown time series type from Wholesale"),
        };
    }

    public static SettlementMethod? Map(AmountPerChargeResultProducedV1.Types.SettlementMethod settlementMethod)
    {
        return settlementMethod switch
        {
            AmountPerChargeResultProducedV1.Types.SettlementMethod.Flex => SettlementMethod.Flex,
            AmountPerChargeResultProducedV1.Types.SettlementMethod.NonProfiled => SettlementMethod.NonProfiled,
            AmountPerChargeResultProducedV1.Types.SettlementMethod.Unspecified => null,
            _ => throw new ArgumentOutOfRangeException(nameof(settlementMethod), settlementMethod, "Unknown time series type from Wholesale"),
        };
    }
}
