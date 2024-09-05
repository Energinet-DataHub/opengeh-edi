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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;

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

    public static string Of(ActorRole actorRole)
    {
        ArgumentNullException.ThrowIfNull(actorRole);

        return actorRole.Code;
    }

    public static string Of(SettlementMethod settlementMethod)
    {
        ArgumentNullException.ThrowIfNull(settlementMethod);

        if (settlementMethod == SettlementMethod.Flex)
            return "D01";
        if (settlementMethod == SettlementMethod.NonProfiled)
            return "E02";

        throw NoCodeFoundFor(settlementMethod.Name);
    }

    public static string Of(MeasurementUnit measurementUnit)
    {
        ArgumentNullException.ThrowIfNull(measurementUnit);

        if (measurementUnit == MeasurementUnit.Kwh)
            return "KWH";
        if (measurementUnit == MeasurementUnit.Pieces)
            return "H87";

        throw NoCodeFoundFor(measurementUnit.Name);
    }

    public static string Of(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        if (resolution == Resolution.QuarterHourly)
            return "PT15M";
        if (resolution == Resolution.Hourly)
            return "PT1H";
        if (resolution == Resolution.Daily)
            return "P1D";
        if (resolution == Resolution.Monthly)
            return "P1M";

        throw NoCodeFoundFor(resolution.Name);
    }

    public static string? ForEnergyResultOf(CalculatedQuantityQuality calculatedQuantityQuality)
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

    public static string? ForWholesaleServicesOf(CalculatedQuantityQuality calculatedQuantityQuality)
    {
        return calculatedQuantityQuality switch
        {
            CalculatedQuantityQuality.Estimated => QuantityQualityCodeCalculated,
            CalculatedQuantityQuality.Incomplete => QuantityQualityCodeCalculated,
            CalculatedQuantityQuality.Measured => QuantityQualityCodeCalculated,
            CalculatedQuantityQuality.Calculated => QuantityQualityCodeCalculated,
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

    public static string Of(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        if (currency == Currency.DanishCrowns)
            return "DKK";

        throw NoCodeFoundFor(currency.Name);
    }

    public static string Of(ChargeType chargeType)
    {
        ArgumentNullException.ThrowIfNull(chargeType);

        if (chargeType == ChargeType.Subscription)
            return "D01";
        if (chargeType == ChargeType.Fee)
            return "D02";
        if (chargeType == ChargeType.Tariff)
            return "D03";

        throw NoCodeFoundFor(chargeType.Name);
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
