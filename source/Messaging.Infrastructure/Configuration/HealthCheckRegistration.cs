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
using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.App.FunctionApp.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Infrastructure.Configuration;

public static class HealthCheckRegistration
{
    public static void AddSqlServerHealthCheck(this IServiceCollection services,  string dbConnectionString)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                name: "MarketRolesDB",
                connectionString: dbConnectionString);
    }

    public static void AddInternalDomainServiceBusQueuesHealthCheck(this IServiceCollection services, string serviceBusConnectionString, [NotNull] params string[] queueNames)
    {
        foreach (var name in queueNames)
        {
            services.AddHealthChecks()
                .AddAzureServiceBusQueue(
                    name: name + "Exists" + Guid.NewGuid(),
                    connectionString: serviceBusConnectionString,
                    queueName: name);
        }
    }

    public static void AddExternalServiceBusSubscriptionsHealthCheck(
        this IServiceCollection services,
        string serviceBusConnectionString,
        [NotNull] string topicName,
        [NotNull] params string[] subscriptionNames)
    {
        foreach (var name in subscriptionNames)
        {
            services.AddHealthChecks()
                .AddAzureServiceBusSubscription(
                    name: name + "Exists" + Guid.NewGuid(),
                    connectionString: serviceBusConnectionString,
                    topicName: topicName,
                    subscriptionName: name);
        }
    }

    public static void AddLiveHealthCheck(this IServiceCollection services)
    {
        services.AddScoped<IHealthCheckEndpointHandler, HealthCheckEndpointHandler>();
        services.AddHealthChecks()
            .AddLiveCheck();
    }
}
