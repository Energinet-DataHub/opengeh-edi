﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.IntegrationEvents.Application.Configuration;

public static class IntegrationEventExtensions
{
    public static IServiceCollection AddIntegrationEventModule(this IServiceCollection services)
    {
        services.AddTransient<IIntegrationEventProcessor, EnergyResultProducedV2Processor>()
            .AddTransient<IIntegrationEventProcessor, MonthlyAmountPerChargeResultProducedV1Processor>()
            .AddTransient<IIntegrationEventProcessor, AmountPerChargeResultProducedV1Processor>()
            .AddTransient<IIntegrationEventProcessor, ActorActivatedIntegrationEventProcessor>()
            .AddTransient<IIntegrationEventProcessor, GridAreaOwnershipAssignedIntegrationEventProcessor>()
            .AddTransient<IIntegrationEventProcessor, ActorCertificateCredentialsAssignedEventProcessor>()
            .AddTransient<IIntegrationEventProcessor, ActorCertificateCredentialsRemovedEventProcessor>()
            .AddTransient<IReadOnlyDictionary<string, IIntegrationEventProcessor>>(
                sp => sp.GetServices<IIntegrationEventProcessor>()
                    .ToDictionary(m => m.EventTypeToHandle, m => m));

        var integrationEventDescriptors = new List<MessageDescriptor>
        {
            EnergyResultProducedV2.Descriptor,
            ActorActivated.Descriptor,
            GridAreaOwnershipAssigned.Descriptor,
            ActorCertificateCredentialsRemoved.Descriptor,
            ActorCertificateCredentialsAssigned.Descriptor,
            MonthlyAmountPerChargeResultProducedV1.Descriptor,
            AmountPerChargeResultProducedV1.Descriptor,
        };
        services.AddSubscriber<IntegrationEventHandler>(integrationEventDescriptors);
        services.AddTransient<IDataRetention, ReceivedIntegrationEventsRetention>()
            .AddTransient<IReceivedIntegrationEventRepository, ReceivedIntegrationEventRepository>()
            .AddTransient<EnergyResultMessageResultFactory>()
            .AddTransient<IIntegrationEventHandler, IntegrationEventHandler>();
        return services;
    }
}
