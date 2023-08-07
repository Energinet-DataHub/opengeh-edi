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

using Application.IncomingMessages.RequestAggregatedMeasureData;
using Application.Transactions.AggregatedMeasureData;
using Application.Transactions.AggregatedMeasureData.Commands;
using Application.Transactions.AggregatedMeasureData.Notifications;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.AggregatedMeasureData.Events;
using Infrastructure.Transactions.AggregatedMeasureData.Handlers;
using Infrastructure.Wholesale;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Transactions.AggregatedMeasureData;

internal static class RequestedAggregatedMeasureDataConfiguration
{
    public static void Configure(IServiceCollection services)
    {
        services.AddTransient<IRequestHandler<RequestAggregatedMeasureDataTransaction, Unit>, AggregatedMeasureDataRequestHandler>();
        services.AddTransient<IRequestHandler<NotifyWholesaleOfAggregatedMeasureDataRequest, Unit>, RequestAggregatedMeasuredDataFromWholesale>();
        services.AddTransient<IRequestHandler<CreateAggregatedMeasureAggregationResults, Unit>, CreateAggregatedMeasureAggregationResultsHandler>();
        services.AddTransient<IRequestHandler<SendAggregatedMeasureRequestToWholesale, Unit>, SendAggregatedMeasuredDataToWholesale>();
        services.AddTransient<IRequestHandler<AcceptedAggregatedTimeSeries, Unit>, AcceptedAggregatedTimeSeriesFromWholesale>();
        services.AddTransient<INotificationHandler<AggregatedMeasureProcessIsInitialized>, NotifyWholesaleWhenAggregatedMeasureProcessIsInitialized>();
        services.AddTransient<INotificationHandler<AggregatedMeasureProcessWasAccepted>, AcceptedAggregatedMeasureProcessIsAvailable>();
        services.AddTransient<INotificationHandler<AggregatedMeasureProcessIsSending>, SendAggregatedMeasureRequestToWholesaleWhenProcessIsSending>();
        services.AddTransient<INotificationHandler<AggregatedTimeSeriesRequestWasAccepted>, WhenAnAcceptedAggregatedTimeSeriesRequestIsAvailable>();
        services.AddScoped<WholesaleInbox>();
        services.AddScoped<IAggregatedMeasureDataProcessRepository, AggregatedMeasureDataProcessRepository>();
    }
}
