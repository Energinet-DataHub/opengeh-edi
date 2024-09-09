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

using Azure.Identity;
using BuildingBlocks.Application.Extensions.Options;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Builder;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddIntegrationEventsHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddServiceBusTopicSubscriptionDeadLetter(
                sp => sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value.FullyQualifiedNamespace,
                sp => sp.GetRequiredService<IOptions<IntegrationEventsOptions>>().Value.TopicName,
                sp => sp.GetRequiredService<IOptions<IntegrationEventsOptions>>().Value.SubscriptionName,
                _ => new DefaultAzureCredential(),
                "EdiIntegrationEventsDeadLetter",
                [HealthChecksConstants.StatusHealthCheckTag]);

        return services;
    }
}
