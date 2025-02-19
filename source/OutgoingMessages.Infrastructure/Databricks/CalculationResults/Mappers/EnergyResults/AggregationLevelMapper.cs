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

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;

public static class AggregationLevelMapper
{
    public static AggregationLevel Map(
        MeteringPointType? meteringPointType,
        SettlementMethod? settlementMethod,
        string? energySupplierGln,
        string? balanceResponsiblePartyGln)
    {
        if (meteringPointType == MeteringPointType.Consumption
            && settlementMethod == null)
        {
                if (energySupplierGln == null && balanceResponsiblePartyGln == null)
                    return AggregationLevel.GridArea;

                throw new InvalidOperationException("It is only Metered Data Responsible that can request data for consumption without a settlement method." +
                                                    $"Invalid combination of metering point type: '{meteringPointType}' and settlement method {settlementMethod}," +
                                                    $" energy supplier: '{energySupplierGln}'" +
                                                    $" and balance responsible: '{balanceResponsiblePartyGln}'.");
        }

        if (meteringPointType == MeteringPointType.Exchange)
        {
            if (energySupplierGln == null && balanceResponsiblePartyGln == null)
                return AggregationLevel.GridArea;

            throw new InvalidOperationException("It is only Metered Data Responsible that can request data for exchange without a settlement method." +
                                                $"Invalid combination of metering point type: '{meteringPointType}' and settlement method {settlementMethod}," +
                                                $" energy supplier: '{energySupplierGln}'" +
                                                $" and balance responsible: '{balanceResponsiblePartyGln}'.");
        }

        return energySupplierGln switch
        {
            null when balanceResponsiblePartyGln == null => AggregationLevel.GridArea,
            null when balanceResponsiblePartyGln != null => AggregationLevel.BalanceResponsibleAndGridArea,
            _ => AggregationLevel.EnergySupplierAndBalanceResponsibleAndGridArea,
        };
    }
}
