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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.Application.Extensions.DependencyInjection;

public static class HealtCheckExtensions
{
    private const string DatabaseName = "edi-sql-db";

    public static IServiceCollection AddExternalDomainServiceBusQueuesHealthCheck(this IServiceCollection services, string serviceBusConnectionString, params string[] queueNames)
    {
        ArgumentNullException.ThrowIfNull(serviceBusConnectionString);
        ArgumentNullException.ThrowIfNull(queueNames);
        foreach (var name in queueNames)
        {
            services.AddHealthChecks()
                .AddAzureServiceBusQueue(
                    name: name + "Exists",
                    connectionString: serviceBusConnectionString,
                    queueName: name);
        }

        return services;
    }

    public static IServiceCollection AddSqlServerHealthCheck(this IServiceCollection services,  IConfiguration configuration)
    {
        if (SqlServerHealthCheckIsAdded(services))
        {
            return services;
        }

        services
            .AddOptions<SqlDatabaseConnectionOptions>()
            .Bind(configuration)
            .Validate(o => !string.IsNullOrEmpty(o.DB_CONNECTION_STRING), "DB_CONNECTION_STRING must be set");

        var dbConnectionString = configuration.GetConnectionString("DB_CONNECTION_STRING")!;

        services.AddHealthChecks()
            .AddSqlServer(
                name: DatabaseName,
                connectionString: dbConnectionString,
                tags: new[] { $"{DatabaseName}" });

        services.TryAddSingleton<SqlHealthCheckIsAdded>();

        return services;
    }

    private static bool SqlServerHealthCheckIsAdded(IServiceCollection services)
    {
        return services.Any(service => service.ServiceType == typeof(SqlHealthCheckIsAdded));
    }

    private sealed class SqlHealthCheckIsAdded
    {
    }
}
