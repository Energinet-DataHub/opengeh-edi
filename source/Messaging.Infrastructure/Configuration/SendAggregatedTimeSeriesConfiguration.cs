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

using MediatR;
using Messaging.Application.Transactions.Aggregations;
using Messaging.Domain.Transactions.Aggregations;
using Messaging.Infrastructure.Transactions.AggregatedTimeSeries;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Infrastructure.Configuration;

internal static class SendAggregatedTimeSeriesConfiguration
{
    internal static void Configure(IServiceCollection services)
    {
        services.AddTransient<IRequestHandler<StartTransaction, Unit>, StartTransactionHandler>();
        services.AddTransient<IRequestHandler<SendAggregatedTimeSeries, Unit>, SendAggregatedTimeSeriesHandler>();
        services.AddScoped<IAggregatedTimeSeriesTransactions, AggregatedTimeSeriesTransactions>();
        services.AddSingleton<IAggregatedTimeSeriesResults, FakeAggregatedTimeSeriesResults>();
        services.AddSingleton<IGridAreaLookup, FakeGridAreaLookup>();
        services.AddTransient<IRequestHandler<RetrieveAggregationResult, Unit>, RetrieveAggregationResultHandler>();
    }
}
