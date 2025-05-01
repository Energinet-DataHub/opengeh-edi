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

using DarkLoop.Azure.Functions.Authorization;
using Energinet.DataHub.EDI.B2BApi.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.Configuration;

namespace Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;

public static class SubsystemAuthenticationExtensions
{
    /// <summary>
    /// Register services necessary for enabling an Azure Function App (isolated worker model)
    /// to use JWT Bearer authentication for HttpTrigger's. This is using DarkLoop Authorization extension.
    ///
    /// Expects <see cref="AuthenticationOptions"/> has been configured in <see cref="AuthenticationOptions.SectionName"/>.
    /// </summary>
    internal static IServiceCollection AddSubsystemAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var authenticationOptions = configuration
            .GetRequiredSection(AuthenticationOptions.SectionName)
            .Get<AuthenticationOptions>();

        if (authenticationOptions == null)
            throw new InvalidOperationException("Missing authentication configuration.");

        GuardAuthenticationOptions(authenticationOptions);

        services
            .AddFunctionsAuthentication(JwtFunctionsBearerDefaults.AuthenticationScheme)
            .AddJwtFunctionsBearer(options =>
            {
                options.Audience = authenticationOptions.ApplicationIdUri;
                options.Authority = authenticationOptions.Issuer;
            });

        return services;
    }

    private static void GuardAuthenticationOptions(AuthenticationOptions authenticationOptions)
    {
        if (string.IsNullOrWhiteSpace(authenticationOptions.ApplicationIdUri))
            throw new InvalidConfigurationException($"Missing '{nameof(AuthenticationOptions.ApplicationIdUri)}'.");
        if (string.IsNullOrWhiteSpace(authenticationOptions.Issuer))
            throw new InvalidConfigurationException($"Missing '{nameof(AuthenticationOptions.Issuer)}'.");
    }
}
