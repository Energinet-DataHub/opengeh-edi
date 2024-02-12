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

public static class CalculationTypeMapper
{
    public static BusinessReason MapCalculationType(EnergyResultProducedV2.Types.CalculationType processType)
    {
        return processType switch
        {
            EnergyResultProducedV2.Types.CalculationType.Aggregation => BusinessReason.PreliminaryAggregation,
            EnergyResultProducedV2.Types.CalculationType.BalanceFixing => BusinessReason.BalanceFixing,
            EnergyResultProducedV2.Types.CalculationType.WholesaleFixing => BusinessReason.WholesaleFixing,
            EnergyResultProducedV2.Types.CalculationType.FirstCorrectionSettlement => BusinessReason.Correction,
            EnergyResultProducedV2.Types.CalculationType.SecondCorrectionSettlement => BusinessReason.Correction,
            EnergyResultProducedV2.Types.CalculationType.ThirdCorrectionSettlement => BusinessReason.Correction,
            EnergyResultProducedV2.Types.CalculationType.Unspecified => throw new InvalidOperationException(
                "Process type is not specified from Wholesales"),
            _ => throw new InvalidOperationException("Unknown process type from Wholesales"),
        };
    }
}
