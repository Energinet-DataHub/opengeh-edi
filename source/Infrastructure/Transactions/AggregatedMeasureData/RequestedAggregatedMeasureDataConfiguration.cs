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
using CimMessageAdapter.Messages;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Infrastructure.Transactions.AggregatedMeasureData.Commands;
using Infrastructure.Transactions.AggregatedMeasureData.Commands.Handlers;
using Infrastructure.Transactions.AggregatedMeasureData.Notifications;
using Infrastructure.Transactions.AggregatedMeasureData.Notifications.Handlers;
using Infrastructure.Wholesale;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Transactions.AggregatedMeasureData;

internal static class RequestedAggregatedMeasureDataConfiguration
{
    public static void Configure(IServiceCollection services)
    {
        services.AddTransient<IRequestHandler<RequestAggregatedMeasureDataTransactionCommand, Unit>, AggregatedMeasureDataRequestHandler>();
#pragma warning disable CS0618 // Type or member is obsolete
        services.AddTransient<IRequestHandler<CreateAggregatedMeasureAggregationResults, Unit>, MakeAggregatedMeasureAsAggregationResults>();
#pragma warning restore CS0618 // Type or member is obsolete
        services.AddTransient<IRequestHandler<SendAggregatedMeasureRequestToWholesale, Unit>, SendAggregatedMeasuredDataToWholesale>();
        services.AddTransient<IRequestHandler<AcceptedAggregatedTimeSeries, Unit>, AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable>();
        services.AddTransient<IRequestHandler<RejectedAggregatedTimeSeries, Unit>, RejectProcessWhenRejectedAggregatedTimeSeriesIsAvailable>();
#pragma warning disable CS0618 // Type or member is obsolete
        services.AddTransient<IRequestHandler<ForwardRejectedAggregationResult, Unit>, ForwardRejectedAggregationResultHandler>();
#pragma warning restore CS0618 // Type or member is obsolete
        services.AddTransient<INotificationHandler<AggregatedMeasureProcessIsInitialized>, NotifyWholesaleWhenAggregatedMeasureProcessIsInitialized>();
        services.AddTransient<IRequestHandler<ReceiveAggregatedMeasureDataRequestCommand, Result>, ValidateAggregatedMeasureDataRequestHandler>();
        services.AddTransient<INotificationHandler<AggregatedTimeSeriesRequestWasAccepted>, WhenAnAcceptedAggregatedTimeSeriesRequestIsAvailable>();
        services.AddTransient<INotificationHandler<AggregatedTimeSeriesRequestWasRejected>, WhenAnRejectedAggregatedTimeSeriesRequestIsAvailable>();
        services.AddScoped<WholesaleInbox>();
        services.AddScoped<IAggregatedMeasureDataProcessRepository, AggregatedMeasureDataProcessRepository>();
        services.AddScoped<AggregatedMeasureDataResponseFactory>();
    }
}
