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

using Application.Configuration.TimeEvents;
using Infrastructure.Transactions.Aggregations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Configuration.IntegrationEvents;

public static class IntegrationEventsConfiguration
{
    public static void Configure(IServiceCollection services)
    {
        services.AddTransient<INotificationHandler<TenSecondsHasHasPassed>,
            ProcessIntegrationEventsOnTenSecondsHasPassed>();
        services.AddTransient<INotificationHandler<ADayHasPassed>, RemoveIntegrationEventsWhenADaysHasPassed>();
        services.AddTransient<INotificationHandler<ADayHasPassed>,
            RemoveMonthOldReceivedIntegrationEventIdsWhenADaysHasPassed>();
        services.AddTransient<IntegrationEventReceiver>();
        services.AddTransient<IntegrationEventsProcessor>();
        services.AddTransient<IIntegrationEventMapper, CalculationResultCompletedEventMapper>();
    }
}
