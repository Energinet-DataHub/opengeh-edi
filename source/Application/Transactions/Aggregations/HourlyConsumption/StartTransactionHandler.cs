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
using System.Threading;
using System.Threading.Tasks;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using MediatR;
using NodaTime;
using Period = Domain.Transactions.Aggregations.Period;

namespace Application.Transactions.Aggregations.HourlyConsumption;

public class StartTransactionHandler : IRequestHandler<StartTransaction>
{
    private readonly IAggregationResults _aggregationResults;
    private readonly IAggregationResultForwardingRepository _repository;

    public StartTransactionHandler(IAggregationResults aggregationResults, IAggregationResultForwardingRepository repository)
    {
        _aggregationResults = aggregationResults;
        _repository = repository;
    }

    public async Task<Unit> Handle(StartTransaction request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aggregationResult = await _aggregationResults.HourlyConsumptionForAsync(
                request.ResultId,
                request.GridArea,
                ActorNumber.Create(request.EnergySupplierNumber))
            .ConfigureAwait(false);

        var transaction = new AggregationResultForwarding(
            TransactionId.New(),
            ActorNumber.Create(request.EnergySupplierNumber),
            MarketRole.EnergySupplier,
            ProcessType.BalanceFixing,
            new Period(
                SystemClock.Instance.GetCurrentInstant(),
                SystemClock.Instance.GetCurrentInstant()));

        _repository.Add(transaction);

        return Unit.Value;
    }
}
