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

using Azure.Messaging.ServiceBus;
using BuildingBlocks.Application.Extensions.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Application.Extensions.DependencyInjection;

public static class ServiceBusExtensions
{
    public static IServiceCollection AddServiceBus(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ServiceBusOptions>()
            .BindConfiguration(ServiceBusOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddSingleton<ServiceBusClient>(provider => new ServiceBusClient(
            provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value.SendConnectionString,
            new ServiceBusClientOptions()
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets,
            }));

        services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();

        return services;
    }
}
