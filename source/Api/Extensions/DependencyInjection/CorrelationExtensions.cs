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
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
using Energinet.DataHub.EDI.Api.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Api.Extensions.DependencyInjection;

public static class CorrelationExtensions
{
    public static IServiceCollection AddCorrelation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EnvironmentOptions>()
            .Bind(configuration)
            .Validate(
                o => !string.IsNullOrEmpty(o.AZURE_FUNCTIONS_ENVIRONMENT),
                "AZURE_FUNCTIONS_ENVIRONMENT must be set");

        var options = configuration.Get<EnvironmentOptions>()!;

        services.AddScoped<ICorrelationContext>(
            _ =>
            {
                var correlationContext = new CorrelationContext();
                if (IsRunningLocal(options)) return correlationContext;
                correlationContext.SetId(Guid.NewGuid().ToString());

                return correlationContext;
            });
        return services;
    }

    private static bool IsRunningLocal(EnvironmentOptions options)
    {
        return options.AZURE_FUNCTIONS_ENVIRONMENT == "Development";
    }
}
