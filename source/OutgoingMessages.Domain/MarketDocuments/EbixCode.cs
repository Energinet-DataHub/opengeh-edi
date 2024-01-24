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

public static class EbixCode
{
    public const string QuantityQualityCodeMeasured = "E01";
    public const string QuantityQualityCodeEstimated = "56";
    public const string QuantityQualityCodeCalculated = "D01";

    public static string Of(BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);

        if (businessReason == BusinessReason.BalanceFixing)
            return "D04";

        if (businessReason == BusinessReason.MoveIn)
            return "E65";

        if (businessReason == BusinessReason.PreliminaryAggregation)
            return "D03";

        if (businessReason == BusinessReason.WholesaleFixing)
            return "D05";

        if (businessReason == BusinessReason.Correction)
            return "D32";

        throw NoCodeFoundFor(businessReason.Name);
    }

    public static BusinessReason To(string businessReasonCode)
    {
        ArgumentNullException.ThrowIfNull(businessReasonCode);

        if (businessReasonCode == "D04")
            return BusinessReason.BalanceFixing;

        if (businessReasonCode == "E65")
            return BusinessReason.MoveIn;

        if (businessReasonCode == "D03")
            return BusinessReason.PreliminaryAggregation;

        if (businessReasonCode == "D05")
            return BusinessReason.WholesaleFixing;

        if (businessReasonCode == "D32")
            return BusinessReason.Correction;

        throw NoBusinessReasonFoundFor(businessReasonCode);
    }

    public static string Of(SettlementVersion settlementVersion)
    {
        ArgumentNullException.ThrowIfNull(settlementVersion);

        if (settlementVersion == SettlementVersion.FirstCorrection)
            return "D01";

        if (settlementVersion == SettlementVersion.SecondCorrection)
            return "D02";

        if (settlementVersion == SettlementVersion.ThirdCorrection)
            return "D03";

        throw NoCodeFoundFor(settlementVersion.Name);
    }

    public static string Of(MeteringPointType meteringPointType)
    {
        ArgumentNullException.ThrowIfNull(meteringPointType);

        if (meteringPointType == MeteringPointType.Consumption)
            return "E17";

        if (meteringPointType == MeteringPointType.Production)
            return "E18";

        if (meteringPointType == MeteringPointType.Exchange)
            return "E20";

        throw NoCodeFoundFor(meteringPointType.Name);
    }

    public static string Of(MarketRole marketRole)
    {
        ArgumentNullException.ThrowIfNull(marketRole);

        return marketRole.Code;
    }

    public static string Of(SettlementType settlementType)
    {
        ArgumentNullException.ThrowIfNull(settlementType);

        if (settlementType == SettlementType.Flex)
            return "D01";
        if (settlementType == SettlementType.NonProfiled)
            return "E02";

        throw NoCodeFoundFor(settlementType.Name);
    }

    public static string Of(MeasurementUnit measurementUnit)
    {
        ArgumentNullException.ThrowIfNull(measurementUnit);

        if (measurementUnit == MeasurementUnit.Kwh)
            return "KWH";

        throw NoCodeFoundFor(measurementUnit.Name);
    }

    public static string Of(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        if (resolution == Resolution.QuarterHourly)
            return "PT15M";
        if (resolution == Resolution.Hourly)
            return "PT1H";

        throw NoCodeFoundFor(resolution.Name);
    }

    public static string? Of(CalculatedQuantityQuality calculatedQuantityQuality)
    {
        return calculatedQuantityQuality switch
        {
            CalculatedQuantityQuality.Estimated => QuantityQualityCodeEstimated,
            CalculatedQuantityQuality.Incomplete => QuantityQualityCodeEstimated,
            CalculatedQuantityQuality.Measured => QuantityQualityCodeMeasured,
            CalculatedQuantityQuality.Calculated => QuantityQualityCodeMeasured,
            CalculatedQuantityQuality.Missing => null,
            CalculatedQuantityQuality.NotAvailable => null,
            _ => throw NoCodeFoundFor(calculatedQuantityQuality.ToString()),
        };
    }

    public static string Of(ReasonCode reasonCode)
    {
        ArgumentNullException.ThrowIfNull(reasonCode);

        if (reasonCode == ReasonCode.FullyAccepted)
            return "39";
        if (reasonCode == ReasonCode.FullyRejected)
            return "41";

        throw NoCodeFoundFor(reasonCode.Name);
    }

    private static InvalidOperationException NoCodeFoundFor(string domainType)
    {
        return new InvalidOperationException($"No code has been defined for {domainType}");
    }

    private static InvalidOperationException NoBusinessReasonFoundFor(string businessReasonCode)
    {
        return new InvalidOperationException($"No business reason has been defined for {businessReasonCode}");
    }
}
