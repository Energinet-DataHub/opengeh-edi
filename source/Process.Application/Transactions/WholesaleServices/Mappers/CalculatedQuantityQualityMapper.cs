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

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Mappers;

public static class CalculatedQuantityQualityMapper
{
    public static Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.CalculatedQuantityQuality QuantityQualityCollectionToEdiQuality(CalculatedQuantityQuality quantityQuality)
    {
        ArgumentNullException.ThrowIfNull(quantityQuality);

        return quantityQuality switch
        {
            CalculatedQuantityQuality.Missing => BuildingBlocks.Domain.Models.CalculatedQuantityQuality.Missing,
            CalculatedQuantityQuality.Estimated => BuildingBlocks.Domain.Models.CalculatedQuantityQuality.Estimated,
            CalculatedQuantityQuality.Measured => BuildingBlocks.Domain.Models.CalculatedQuantityQuality.Measured,
            CalculatedQuantityQuality.Calculated => BuildingBlocks.Domain.Models.CalculatedQuantityQuality.Calculated,
            _ => throw new ArgumentOutOfRangeException(nameof(quantityQuality), quantityQuality, "Unknown quantity quality from Wholesale"),
        };
    }
}
