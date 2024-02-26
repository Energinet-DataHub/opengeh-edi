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

using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Infrastructure.Extensions.DependencyInjection;

public static class IntegrationEventsExtensions
{
    public static IServiceCollection AddIntegrationEvents(this IServiceCollection services)
    {
        services.AddTransient<IDataRetention, ReceivedIntegrationEventsRetention>()
        .AddTransient<IReceivedIntegrationEventRepository, ReceivedIntegrationEventRepository>()
        .AddTransient<IIntegrationEventProcessor, ActorActivatedIntegrationEventProcessor>()
        .AddTransient<IIntegrationEventProcessor, GridAreaOwnershipAssignedIntegrationEventProcessor>()
        .AddTransient<IIntegrationEventProcessor, ActorCertificateCredentialsAssignedEventProcessor>()
        .AddTransient<IIntegrationEventProcessor, ActorCertificateCredentialsRemovedEventProcessor>()
        .AddTransient<IReadOnlyDictionary<string, IIntegrationEventProcessor>>(
            sp => sp.GetServices<IIntegrationEventProcessor>()
                .ToDictionary(m => m.EventTypeToHandle, m => m));

        return services;
    }
}
