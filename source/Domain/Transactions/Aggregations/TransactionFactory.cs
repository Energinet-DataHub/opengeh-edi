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
using Domain.OutgoingMessages;
using Domain.SeedWork;

namespace Domain.Transactions.Aggregations;

public class TransactionFactory
{
    private readonly ActorNumber _gridOperator;

    public TransactionFactory(ActorNumber gridOperator)
    {
        _gridOperator = gridOperator;
    }

    public AggregationResultForwarding CreateFrom(Aggregation result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var processType = EnumerationType.FromName<ProcessType>(result.ProcessType);
        return new AggregationResultForwarding(TransactionId.New(), _gridOperator, MarketRole.MeteredDataResponsible, processType);
    }
}
