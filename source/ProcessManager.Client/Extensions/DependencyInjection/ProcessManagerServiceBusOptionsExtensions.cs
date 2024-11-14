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

using Energinet.DataHub.ProcessManager.Client.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;

public static class ProcessManagerServiceBusOptionsExtensions
{
    public static void AddProcessManagerServiceBusOptions(this IServiceCollection services)
    {
        services
            .AddOptions<ProcessManagerServiceBusOptions>()
            .BindConfiguration(ProcessManagerServiceBusOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public static ProcessManagerServiceBusOptions GetRequiredProcessManagerServiceBusOptions(this IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<ProcessManagerServiceBusOptions>>();
        return options.Value;
    }
}
