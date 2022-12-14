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

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Domain.Transactions.AggregatedTimeSeries;

namespace Messaging.Application.Transactions.AggregatedTimeSeries;

public class SendAggregatedTimeSeriesHandler : IRequestHandler<SendAggregatedTimeSeries, Unit>
{
    private readonly IAggregatedTimeSeriesTransactions _transactions;

    public SendAggregatedTimeSeriesHandler(IAggregatedTimeSeriesTransactions transactions)
    {
        _transactions = transactions;
    }

    public Task<Unit> Handle(SendAggregatedTimeSeries request, CancellationToken cancellationToken)
    {
        var transaction = new AggregatedTimeSeriesTransaction();
        _transactions.Add(transaction);
        return Task.FromResult(Unit.Value);
    }
}

public record SendAggregatedTimeSeries() : ICommand<Unit>;
