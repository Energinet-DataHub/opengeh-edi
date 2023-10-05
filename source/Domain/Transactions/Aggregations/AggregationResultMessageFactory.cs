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

using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.NotifyAggregatedMeasureData;

namespace Energinet.DataHub.EDI.Domain.Transactions.Aggregations;

public static class AggregationResultMessageFactory
{
    public static AggregationResultMessage CreateMessage(Aggregation result, ProcessId processId, IDocumentWriter documentWriter)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (processId == null) throw new ArgumentNullException(nameof(processId));

        if (IsTotalResultPerGridArea(result))
        {
            return MessageForTheGridOperator(result, processId, documentWriter);
        }

        if (ResultIsForTheEnergySupplier(result))
        {
            return MessageForTheEnergySupplier(result, processId, documentWriter);
        }

        if (ResultIsForTheBalanceResponsible(result))
        {
            return MessageForTheBalanceResponsible(result, processId, documentWriter);
        }

        throw new InvalidOperationException("Could not determine the receiver of the aggregation result");
    }

    public static bool ResultIsForTheEnergySupplier(Aggregation result)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(result));
        return result?.ActorGrouping!.EnergySupplierNumber is not null &&
               result.ActorGrouping?.BalanceResponsibleNumber is null;
    }

    public static bool IsTotalResultPerGridArea(Aggregation result)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(result));
        return result?.ActorGrouping?.BalanceResponsibleNumber == null &&
               result?.ActorGrouping?.EnergySupplierNumber == null;
    }

    public static bool ResultIsForTheBalanceResponsible(Aggregation result)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(result));
        return result?.ActorGrouping!.BalanceResponsibleNumber is not null;
    }

    private static AggregationResultMessage MessageForTheGridOperator(Aggregation result, ProcessId processId, IDocumentWriter documentWriter)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(result.GridAreaDetails!.OperatorNumber), MarketRole.MeteredDataResponsible, processId, result, documentWriter);
    }

    private static AggregationResultMessage MessageForTheEnergySupplier(Aggregation result, ProcessId processId, IDocumentWriter documentWriter)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(result.ActorGrouping!.EnergySupplierNumber!), MarketRole.EnergySupplier, processId, result, documentWriter);
    }

    private static AggregationResultMessage MessageForTheBalanceResponsible(Aggregation result, ProcessId processId, IDocumentWriter documentWriter)
    {
        return AggregationResultMessage.Create(ActorNumber.Create(result.ActorGrouping!.BalanceResponsibleNumber!), MarketRole.BalanceResponsibleParty, processId, result, documentWriter);
    }
}
