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
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.Commands.Commands;
using Domain.OutgoingMessages;
using Domain.SeedWork;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using MediatR;

namespace Application.Transactions.Aggregations;

public class RetrieveAggregationResults : InternalCommand
{
    [JsonConstructor]
    public RetrieveAggregationResults(Guid id, Guid resultsId, string aggregationProcess, string gridArea, ZPeriod period)
    : base(id)
    {
        AggregationProcess = aggregationProcess;
        GridArea = gridArea;
        Period = period;
        ResultsId = resultsId;
    }

    public RetrieveAggregationResults(Guid resultsId, string aggregationProcess, string gridArea, ZPeriod period)
    {
        AggregationProcess = aggregationProcess;
        GridArea = gridArea;
        Period = period;
        ResultsId = resultsId;
    }

    public Guid ResultsId { get; }

    public string AggregationProcess { get; }

    public string GridArea { get; }

    public ZPeriod Period { get; }
}

public class RetrieveAggregationResultsHandler : IRequestHandler<RetrieveAggregationResults, Unit>
{
    private readonly TransactionScheduler _transactionScheduler;

    public RetrieveAggregationResultsHandler(TransactionScheduler transactionScheduler)
    {
        _transactionScheduler = transactionScheduler;
    }

    public async Task<Unit> Handle(RetrieveAggregationResults request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _transactionScheduler.ScheduleForAsync(
            request.ResultsId,
            EnumerationType.FromName<ProcessType>(request.AggregationProcess),
            GridArea.Create(request.GridArea),
            new Domain.Transactions.Aggregations.Period(request.Period.Start, request.Period.End)).ConfigureAwait(false);
        return Unit.Value;
    }
}
