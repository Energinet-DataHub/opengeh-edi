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
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.Transactions.Aggregations;

namespace Infrastructure.OutgoingMessages.Common;

public static class CimCode
{
    public static string Of(BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);

        if (businessReason == BusinessReason.BalanceFixing)
            return "D04";

        if (businessReason == BusinessReason.MoveIn)
            return "E65";

        if (businessReason == BusinessReason.PreliminaryAggregation)
            return "D03";

        throw NoCodeFoundFor(businessReason.Name);
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

        if (marketRole == MarketRole.EnergySupplier)
            return "DDQ";
        if (marketRole == MarketRole.GridOperator)
            return "DDM";
        if (marketRole == MarketRole.MeteredDataResponsible)
            return "MDR";
        if (marketRole == MarketRole.MeteringDataAdministrator)
            return "DGL";
        if (marketRole == MarketRole.MeteringPointAdministrator)
            return "DDZ";
        if (marketRole == MarketRole.BalanceResponsibleParty)
            return "DDK";

        throw NoCodeFoundFor(marketRole.Name);
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

    public static string Of(Quality quality)
    {
        ArgumentNullException.ThrowIfNull(quality);

        if (quality == Quality.Estimated)
            return "A03";
        if (quality == Quality.Incomplete)
            return "A05";
        if (quality == Quality.Calculated)
            return "A06";
        if (quality == Quality.Measured)
            return "A04";
        if (quality == Quality.Missing)
            return "A02";

        throw NoCodeFoundFor(quality.Name);
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

    private static Exception NoCodeFoundFor(string domainType)
    {
        return new InvalidOperationException($"No code has been defined for {domainType}");
    }
}
