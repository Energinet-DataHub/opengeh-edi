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

public static class SettlementVersionMapper
{
    public static SettlementVersion? Map(EnergyResultProducedV2.Types.CalculationType calculationType)
    {
        return calculationType switch
        {
            EnergyResultProducedV2.Types.CalculationType.BalanceFixing or
            EnergyResultProducedV2.Types.CalculationType.Aggregation or
            EnergyResultProducedV2.Types.CalculationType.WholesaleFixing => null,
            EnergyResultProducedV2.Types.CalculationType.FirstCorrectionSettlement => SettlementVersion.FirstCorrection,
            EnergyResultProducedV2.Types.CalculationType.SecondCorrectionSettlement => SettlementVersion.SecondCorrection,
            EnergyResultProducedV2.Types.CalculationType.ThirdCorrectionSettlement => SettlementVersion.ThirdCorrection,
            EnergyResultProducedV2.Types.CalculationType.Unspecified => throw new InvalidOperationException("Calculation type is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(calculationType), calculationType, "Unknown calculation type from Wholesale"),
        };
    }

    public static SettlementVersion? Map(MonthlyAmountPerChargeResultProducedV1.Types.CalculationType calculationType)
    {
        return calculationType switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing => null,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.FirstCorrectionSettlement => SettlementVersion.FirstCorrection,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.SecondCorrectionSettlement => SettlementVersion.SecondCorrection,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.ThirdCorrectionSettlement => SettlementVersion.ThirdCorrection,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.Unspecified => throw new InvalidOperationException("Calculation type is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(calculationType), calculationType, "Unknown calculation type from Wholesale"),
        };
    }

    public static SettlementVersion? Map(AmountPerChargeResultProducedV1.Types.CalculationType calculationType)
    {
        return calculationType switch
        {
            AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing => null,
            AmountPerChargeResultProducedV1.Types.CalculationType.FirstCorrectionSettlement => SettlementVersion.FirstCorrection,
            AmountPerChargeResultProducedV1.Types.CalculationType.SecondCorrectionSettlement => SettlementVersion.SecondCorrection,
            AmountPerChargeResultProducedV1.Types.CalculationType.ThirdCorrectionSettlement => SettlementVersion.ThirdCorrection,
            AmountPerChargeResultProducedV1.Types.CalculationType.Unspecified => throw new InvalidOperationException("Calculation type is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(calculationType), calculationType, "Unknown calculation type from Wholesale"),
        };
    }

    public static SettlementVersion? Map(TotalMonthlyAmountResultProducedV1.Types.CalculationType calculationType)
    {
        return calculationType switch
        {
            TotalMonthlyAmountResultProducedV1.Types.CalculationType.WholesaleFixing => null,
            TotalMonthlyAmountResultProducedV1.Types.CalculationType.FirstCorrectionSettlement => SettlementVersion.FirstCorrection,
            TotalMonthlyAmountResultProducedV1.Types.CalculationType.SecondCorrectionSettlement => SettlementVersion.SecondCorrection,
            TotalMonthlyAmountResultProducedV1.Types.CalculationType.ThirdCorrectionSettlement => SettlementVersion.ThirdCorrection,
            TotalMonthlyAmountResultProducedV1.Types.CalculationType.Unspecified => throw new InvalidOperationException("Calculation type is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(calculationType), calculationType, "Unknown calculation type from Wholesale"),
        };
    }
}
