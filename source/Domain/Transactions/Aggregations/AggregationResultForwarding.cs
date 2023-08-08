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
using Domain.SeedWork;

namespace Domain.Transactions.Aggregations;

public class AggregationResultForwarding : Entity
{
    public AggregationResultForwarding(
        TransactionId id)
    {
        Id = id;
    }

    public TransactionId Id { get; }

    public AggregationResultMessage CreateMessage(Aggregation result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Receiver is not null && result.ReceiverRole is not null)
        {
            return MessageForKnownReceiver(result, result.Receiver, result.ReceiverRole);
        }

        if (IsTotalResultPerGridArea(result))
        {
            return MessageForTheGridOperator(result);
        }

        if (ResultIsForTheEnergySupplier(result))
        {
            return MessageForTheEnergySupplier(result);
        }

        if (ResultIsForTheBalanceResponsible(result))
        {
            return MessageForTheBalanceResponsible(result);
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

    private AggregationResultMessage MessageForKnownReceiver(Aggregation result, string receiver, string receiverRole)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(receiver), EnumerationType.FromName<MarketRole>(receiverRole), Id, result);
    }

    private AggregationResultMessage MessageForTheGridOperator(Aggregation result)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(result.GridAreaDetails!.OperatorNumber), MarketRole.MeteredDataResponsible, Id, result);
    }

    private AggregationResultMessage MessageForTheEnergySupplier(Aggregation result)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(result.ActorGrouping!.EnergySupplierNumber!), MarketRole.EnergySupplier, Id, result);
    }

    private AggregationResultMessage MessageForTheBalanceResponsible(Aggregation result)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(result.ActorGrouping!.BalanceResponsibleNumber!), MarketRole.BalanceResponsibleParty, Id, result);
    }
}
