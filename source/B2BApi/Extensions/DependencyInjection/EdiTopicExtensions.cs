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

using Azure.Core;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.B2BApi.Configuration;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021_023;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_028;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;

public static class EdiTopicExtensions
{
    public static IServiceCollection AddEnqueueMessagesFromProcessManager(this IServiceCollection services, TokenCredential credential)
    {
        services
            .AddOptions<EdiTopicOptions>()
            .BindConfiguration(EdiTopicOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddHealthChecks()
            .AddAzureServiceBusTopic(
                fullyQualifiedNamespaceFactory: sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                topicNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.Name,
                tokenCredentialFactory: _ => credential,
                name: "EDI Topic")
            .AddAzureServiceBusSubscription(
                fullyQualifiedNamespaceFactory: sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                topicNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.Name,
                subscriptionNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.EnqueueBrs021AndBrs023SubscriptionName,
                tokenCredentialFactory: _ => credential,
                name: "Enqueue BRS-021/023 Subscription")
            .AddAzureServiceBusSubscription(
                fullyQualifiedNamespaceFactory: sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                topicNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.Name,
                subscriptionNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.EnqueueBrs026SubscriptionName,
                tokenCredentialFactory: _ => credential,
                name: "Enqueue BRS-026 Subscription")
            .AddAzureServiceBusSubscription(
                fullyQualifiedNamespaceFactory: sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                topicNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.Name,
                subscriptionNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.EnqueueBrs026SubscriptionName,
                tokenCredentialFactory: _ => credential,
                name: "Enqueue BRS-028 Subscription");

        services
            .AddTransient<EnqueueBrs021AndBrs023Handler>()
            .AddTransient<EnqueueBrs026Handler>()
            .AddTransient<EnqueueBrs028Handler>();

        return services;
    }
}
