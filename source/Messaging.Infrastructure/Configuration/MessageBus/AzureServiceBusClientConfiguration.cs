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
using System.Collections.Generic;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Infrastructure.Configuration.MessageBus;

public static class AzureServiceBusClientConfiguration
{
    public static IServiceCollection AddAzureServiceBusClient(this IServiceCollection services, ServiceBusClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddAzureClients(
            builder => builder.AddServiceBusClient(configuration.ConnectionString)
                .WithName(configuration.Configuration.ClientRegistrationName));

        services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();

        return services;
    }
}

public record ServiceBusClientConfiguration(string? ConnectionString, IServiceBusClientConfiguration Configuration);
