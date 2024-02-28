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
using System.Collections.Generic;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;

/// <summary>
///     Provides mapping functionality for converting a collection of quantity qualities to EDI quality.
///     The conversion is based on the documentation at
///     https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality.
///     The conversion can summarized as follows. Do note, that as soon as a condition is met, the remaining conditions are
///     not evaluated and the conditions are evaluated in order of appearance.
///     <list type="bullet">
///         <item>
///             <description>
///                 If the collection contains Missing and doesn't contain Estimated, Measured, or Calculated, it
///                 returns CalculatedQuantityQuality.Missing.
///             </description>
///         </item>
///         <item>
///             <description>
///                 If the collection contains Missing, regardless of the other values, it returns
///                 CalculatedQuantityQuality.Incomplete.
///             </description>
///         </item>
///         <item>
///             <description>
///                 If the collection contains Estimated, regardless of the other values, it returns
///                 CalculatedQuantityQuality.Estimated.
///             </description>
///         </item>
///         <item>
///             <description>
///                 If the collection contains Measured, regardless of the other values, it returns
///                 CalculatedQuantityQuality.Measured.
///             </description>
///         </item>
///         <item>
///             <description>
///                 If the collection contains Calculated, regardless of the other values, it returns
///                 CalculatedQuantityQuality.Calculated.
///             </description>
///         </item>
///         <item>
///             <description>If none of the above conditions are met, it returns CalculatedQuantityQuality.NotAvailable.</description>
///         </item>
///     </list>
/// </summary>
public static class CalculatedQuantityQualityMapper
{
    /// <summary>
    ///     Converts a collection of quantity qualities to EDI quality.
    /// </summary>
    /// <param name="quantityQualities">The collection of quantity qualities to convert.</param>
    /// <returns>The calculated quantity quality based on the input collection.</returns>
    public static CalculatedQuantityQuality QuantityQualityCollectionToEdiQuality(
        ICollection<EnergyResultProducedV2.Types.QuantityQuality> quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities);

        return (missing: quantityQualities.Contains(EnergyResultProducedV2.Types.QuantityQuality.Missing),
                estimated: quantityQualities.Contains(EnergyResultProducedV2.Types.QuantityQuality.Estimated),
                measured: quantityQualities.Contains(EnergyResultProducedV2.Types.QuantityQuality.Measured),
                calculated: quantityQualities.Contains(EnergyResultProducedV2.Types.QuantityQuality.Calculated)) switch
            {
                (missing: true, estimated: false, measured: false, calculated: false) => CalculatedQuantityQuality.Missing,
                (missing: true, _, _, _) => CalculatedQuantityQuality.Incomplete,
                (_, estimated: true, _, _) => CalculatedQuantityQuality.Estimated,
                (_, _, measured: true, _) => CalculatedQuantityQuality.Measured,
                (_, _, _, calculated: true) => CalculatedQuantityQuality.Calculated,
                _ => CalculatedQuantityQuality.NotAvailable,
            };
    }

    /// <summary>
    ///     Converts a collection of quantity qualities to EDI quality.
    /// </summary>
    /// <param name="quantityQualities">The collection of quantity qualities to convert.</param>
    /// <returns>The calculated quantity quality based on the input collection.</returns>
    public static CalculatedQuantityQuality QuantityQualityCollectionToEdiQuality(
        ICollection<QuantityQuality> quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities);

        return (missing: quantityQualities.Contains(QuantityQuality.Missing),
                estimated: quantityQualities.Contains(QuantityQuality.Estimated),
                measured: quantityQualities.Contains(QuantityQuality.Measured),
                calculated: quantityQualities.Contains(QuantityQuality.Calculated)) switch
            {
                (missing: true, estimated: false, measured: false, calculated: false) => CalculatedQuantityQuality.Missing,
                (missing: true, _, _, _) => CalculatedQuantityQuality.Incomplete,
                (_, estimated: true, _, _) => CalculatedQuantityQuality.Estimated,
                (_, _, measured: true, _) => CalculatedQuantityQuality.Measured,
                (_, _, _, calculated: true) => CalculatedQuantityQuality.Calculated,
                _ => CalculatedQuantityQuality.NotAvailable,
            };
    }

    public static CalculatedQuantityQuality QuantityQualityCollectionToEdiQuality(
        ICollection<AmountPerChargeResultProducedV1.Types.QuantityQuality> quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities);

        return (missing: quantityQualities.Contains(AmountPerChargeResultProducedV1.Types.QuantityQuality.Missing),
                estimated: quantityQualities.Contains(AmountPerChargeResultProducedV1.Types.QuantityQuality.Estimated),
                measured: quantityQualities.Contains(AmountPerChargeResultProducedV1.Types.QuantityQuality.Measured),
                calculated: quantityQualities.Contains(AmountPerChargeResultProducedV1.Types.QuantityQuality.Calculated)) switch
            {
                (missing: true, estimated: false, measured: false, calculated: false) => CalculatedQuantityQuality.Missing,
                (missing: true, _, _, _) => CalculatedQuantityQuality.Incomplete,
                (_, estimated: true, _, _) => CalculatedQuantityQuality.Estimated,
                (_, _, measured: true, _) => CalculatedQuantityQuality.Measured,
                (_, _, _, calculated: true) => CalculatedQuantityQuality.Calculated,
                _ => CalculatedQuantityQuality.NotAvailable,
            };
    }
}
