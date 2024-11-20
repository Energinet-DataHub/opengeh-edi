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

using Energinet.DataHub.ProcessManager.Orchestrations.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.ProcessManager.Orchestrations.Extensions.DependencyInjection;

public static class ProcessManagerTopicExtensions
{
    /// <summary>
    /// Add required dependencies to use the Process Manager Service Bus topic.
    /// </summary>
    public static IServiceCollection AddProcessManagerTopic(this IServiceCollection services)
    {
        services
            .AddOptionsWithValidateOnStart<ProcessManagerTopicOptions>()
            .BindConfiguration(ProcessManagerTopicOptions.SectionName)
            .ValidateDataAnnotations();

        return services;
    }
}
