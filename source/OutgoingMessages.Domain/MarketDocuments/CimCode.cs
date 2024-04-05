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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;

public static class CimCode
{
    public const string QuantityQualityCodeIncomplete = "A05";
    public const string QuantityQualityCodeEstimated = "A03";
    public const string QuantityQualityCodeMeasured = "A04";
    public const string QuantityQualityCodeCalculated = "A06";
    public const string QuantityQualityCodeNotAvailable = "A02";

    public static string ForEnergyResultOf(CalculatedQuantityQuality calculatedQuantityQuality)
    {
        return calculatedQuantityQuality switch
        {
            CalculatedQuantityQuality.Missing => QuantityQualityCodeIncomplete,
            CalculatedQuantityQuality.Incomplete => QuantityQualityCodeIncomplete,
            CalculatedQuantityQuality.Estimated => QuantityQualityCodeEstimated,
            CalculatedQuantityQuality.Measured => QuantityQualityCodeMeasured,
            CalculatedQuantityQuality.Calculated => QuantityQualityCodeCalculated,
            CalculatedQuantityQuality.NotAvailable => QuantityQualityCodeNotAvailable,
            _ => throw NoCodeFoundFor(calculatedQuantityQuality.ToString()),
        };
    }

    public static string ForWholesaleServicesOf(CalculatedQuantityQuality calculatedQuantityQuality)
    {
        return calculatedQuantityQuality switch
        {
            CalculatedQuantityQuality.Missing => QuantityQualityCodeIncomplete,
            CalculatedQuantityQuality.Incomplete => QuantityQualityCodeIncomplete,
            CalculatedQuantityQuality.Estimated => QuantityQualityCodeCalculated,
            CalculatedQuantityQuality.Measured => QuantityQualityCodeCalculated,
            CalculatedQuantityQuality.Calculated => QuantityQualityCodeCalculated,
            CalculatedQuantityQuality.NotAvailable => QuantityQualityCodeNotAvailable,
            _ => throw NoCodeFoundFor(calculatedQuantityQuality.ToString()),
        };
    }

    public static string CodingSchemeOf(ActorNumber actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        if (ActorNumber.IsGlnNumber(actorNumber.Value))
            return "A10";
        if (ActorNumber.IsEic(actorNumber.Value))
            return "A01";

        throw NoCodeFoundFor(actorNumber.Value);
    }

    private static InvalidOperationException NoCodeFoundFor(string domainType)
    {
        return new InvalidOperationException($"No code has been defined for {domainType}");
    }
}
