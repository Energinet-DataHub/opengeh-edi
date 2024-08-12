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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Factories;

public class QuantityQualitiesFactor
{
    /// <summary>
    /// Quantity quality mappings is defined by the business.
    /// See "https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality" for more information.
    /// </summary>
    public static CalculatedQuantityQuality CreateQuantityQuality(decimal? price, IReadOnlyCollection<QuantityQuality> qualities, ChargeType? chargeType)
    {
        if (price == null)
        {
            return CalculatedQuantityQuality.Incomplete;
        }

        if (chargeType == ChargeType.Subscription || chargeType == ChargeType.Fee)
        {
            return CalculatedQuantityQuality.Calculated;
        }

        return MapQuantityQualitiesToQuality(qualities);
    }

    private static CalculatedQuantityQuality MapQuantityQualitiesToQuality(
        IReadOnlyCollection<QuantityQuality> qualities)
    {
        ArgumentNullException.ThrowIfNull(qualities);

        return (missing: qualities.Contains(QuantityQuality.Missing),
                estimated: qualities.Contains(QuantityQuality.Estimated),
                measured: qualities.Contains(QuantityQuality.Measured),
                calculated: qualities.Contains(QuantityQuality.Calculated)) switch
            {
                (missing: true, estimated: false, measured: false, calculated: false) => CalculatedQuantityQuality.Missing,
                (missing: true, _, _, _) => CalculatedQuantityQuality.Incomplete,
                (_, estimated: true, _, _) => CalculatedQuantityQuality.Calculated,
                (_, _, measured: true, _) => CalculatedQuantityQuality.Calculated,
                (_, _, _, calculated: true) => CalculatedQuantityQuality.Calculated,
                _ => CalculatedQuantityQuality.NotAvailable,
            };
    }
}
