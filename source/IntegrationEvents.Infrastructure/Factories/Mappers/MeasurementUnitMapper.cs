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
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;

public static class MeasurementUnitMapper
{
    public static MeasurementUnit Map(EnergyResultProducedV2.Types.QuantityUnit quantityUnit)
    {
        return quantityUnit switch
        {
            EnergyResultProducedV2.Types.QuantityUnit.Kwh => MeasurementUnit.Kwh,
            EnergyResultProducedV2.Types.QuantityUnit.Unspecified => throw new InvalidOperationException("Quantity unit is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(quantityUnit), quantityUnit, "Unknown quantity unit from Wholesale"),
        };
    }

    public static MeasurementUnit Map(MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit quantityUnit)
    {
        return quantityUnit switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh => MeasurementUnit.Kwh,
            MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Pieces => MeasurementUnit.Pieces,
            MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Unspecified => throw new InvalidOperationException("Quantity unit is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(quantityUnit), quantityUnit, "Unknown quantity unit from Wholesale"),
        };
    }

    public static MeasurementUnit Map(AmountPerChargeResultProducedV1.Types.QuantityUnit quantityUnit)
    {
        return quantityUnit switch
        {
            AmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh => MeasurementUnit.Kwh,
            AmountPerChargeResultProducedV1.Types.QuantityUnit.Pieces => MeasurementUnit.Pieces,
            AmountPerChargeResultProducedV1.Types.QuantityUnit.Unspecified => throw new InvalidOperationException("Quantity unit is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(quantityUnit), quantityUnit, "Unknown quantity unit from Wholesale"),
        };
    }
}
