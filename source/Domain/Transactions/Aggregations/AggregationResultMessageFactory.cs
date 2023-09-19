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

using Domain.Actors;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;

namespace Domain.Transactions.Aggregations;

public static class AggregationResultMessageFactory
{
    public static AggregationResultMessage CreateMessage(Aggregation result, ProcessId processId)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (processId == null) throw new ArgumentNullException(nameof(processId));

        if (IsTotalResultPerGridArea(result))
        {
            return MessageForTheGridOperator(result, processId);
        }

        if (ResultIsForTheEnergySupplier(result))
        {
            return MessageForTheEnergySupplier(result, processId);
        }

        if (ResultIsForTheBalanceResponsible(result))
        {
            return MessageForTheBalanceResponsible(result, processId);
        }

        throw new InvalidOperationException("Could not determine the receiver of the aggregation result");
    }

    private static bool ResultIsForTheEnergySupplier(Aggregation result)
    {
        return result.ActorGrouping!.EnergySupplierNumber is not null &&
               result.ActorGrouping?.BalanceResponsibleNumber is null;
    }

    private static bool IsTotalResultPerGridArea(Aggregation result)
    {
        return result.ActorGrouping?.BalanceResponsibleNumber == null &&
               result.ActorGrouping?.EnergySupplierNumber == null;
    }

    private static bool ResultIsForTheBalanceResponsible(Aggregation result)
    {
        return result.ActorGrouping!.BalanceResponsibleNumber is not null;
    }

    private static AggregationResultMessage MessageForTheGridOperator(Aggregation result, ProcessId processId)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(result.GridAreaDetails!.OperatorNumber), MarketRole.MeteredDataResponsible, processId, result);
    }

    private static AggregationResultMessage MessageForTheEnergySupplier(Aggregation result, ProcessId processId)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(result.ActorGrouping!.EnergySupplierNumber!), MarketRole.EnergySupplier, processId, result);
    }

    private static AggregationResultMessage MessageForTheBalanceResponsible(Aggregation result, ProcessId processId)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(result.ActorGrouping!.BalanceResponsibleNumber!), MarketRole.BalanceResponsibleParty, processId, result);
    }
}
