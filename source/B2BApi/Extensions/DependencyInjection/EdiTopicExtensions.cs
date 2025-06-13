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

using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.Common.Identity;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.B2BApi.Configuration;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_024;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_025;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_028;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_045;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;

public static class EdiTopicExtensions
{
    /// <summary>
    /// Register services and health checks for enqueue actor message from process manager.
    /// </summary>
    /// <remarks>
    /// Expects "AddTokenCredentialProvider" has been called to register <see cref="TokenCredentialProvider"/>.
    /// </remarks>
    public static IServiceCollection AddEnqueueActorMessagesFromProcessManager(this IServiceCollection services)
    {
        services
            .AddOptions<EdiTopicOptions>()
            .BindConfiguration(EdiTopicOptions.SectionName)
            .ValidateDataAnnotations();

        services
            .AddHealthChecks()
            .AddAzureServiceBusTopic(
                fullyQualifiedNamespaceFactory: sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                topicNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.Name,
                tokenCredentialFactory: sp => sp.GetRequiredService<TokenCredentialProvider>().Credential,
                name: "EDI Topic")
            .AddAzureServiceBusSubscription(
                fullyQualifiedNamespaceFactory: sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                topicNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.Name,
                subscriptionNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.EnqueueBrs_023_027_SubscriptionName,
                tokenCredentialFactory: sp => sp.GetRequiredService<TokenCredentialProvider>().Credential,
                name: "Enqueue BRS-023/027 Subscription")
            .AddAzureServiceBusSubscription(
                fullyQualifiedNamespaceFactory: sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                topicNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.Name,
                subscriptionNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.EnqueueBrs_026_SubscriptionName,
                tokenCredentialFactory: sp => sp.GetRequiredService<TokenCredentialProvider>().Credential,
                name: "Enqueue BRS-026 Subscription")
            .AddAzureServiceBusSubscription(
                fullyQualifiedNamespaceFactory: sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                topicNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.Name,
                subscriptionNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.EnqueueBrs_028_SubscriptionName,
                tokenCredentialFactory: sp => sp.GetRequiredService<TokenCredentialProvider>().Credential,
                name: "Enqueue BRS-028 Subscription")
            .AddAzureServiceBusSubscription(
                fullyQualifiedNamespaceFactory: sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                topicNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.Name,
                subscriptionNameFactory: sp => sp.GetRequiredService<IOptions<EdiTopicOptions>>().Value.EnqueueBrs_021_Forward_Metered_Data_SubscriptionName,
                tokenCredentialFactory: sp => sp.GetRequiredService<TokenCredentialProvider>().Credential,
                name: "Enqueue BRS-021 Forward Metered Data Subscription");

        services
            .AddTransient<EnqueueHandler_Brs_023_027_V1>()
            .AddTransient<EnqueueHandler_Brs_024_V1>()
            .AddTransient<EnqueueHandler_Brs_025_V1>()
            .AddTransient<EnqueueHandler_Brs_026_V1>()
            .AddTransient<EnqueueHandler_Brs_028_V1>()
            .AddTransient<EnqueueHandler_Brs_021_ForwardMeteredData_V1>()
            .AddTransient<EnqueueHandler_Brs_021_CalculatedMeasurements_V1>()
            .AddTransient<EnqueueHandler_Brs_045_MissingMeasurementsLog>();

        return services;
    }
}
