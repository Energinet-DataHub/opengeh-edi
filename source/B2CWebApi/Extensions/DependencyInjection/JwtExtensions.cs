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

using Energinet.DataHub.Core.App.WebApp.Authentication;
using Energinet.DataHub.Core.App.WebApp.Authorization;
using Energinet.DataHub.EDI.B2CWebApi.Extensions.Options;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;

namespace Energinet.DataHub.EDI.B2CWebApi.Extensions.DependencyInjection;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtTokenSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration)
            .Validate(
                o => !string.IsNullOrEmpty(o.EXTERNAL_OPEN_ID_URL),
                "EXTERNAL_OPEN_ID_URL must be set")
            .Validate(
                o => !string.IsNullOrEmpty(o.INTERNAL_OPEN_ID_URL),
                "INTERNAL_OPEN_ID_URL must be set")
            .Validate(
                o => !string.IsNullOrEmpty(o.BACKEND_BFF_APP_ID),
                "BACKEND_BFF_APP_ID must be set");

        var options = configuration.Get<JwtOptions>()!;
        services.AddJwtBearerAuthentication(options.EXTERNAL_OPEN_ID_URL, options.INTERNAL_OPEN_ID_URL, options.BACKEND_BFF_APP_ID);
        services.AddUserAuthentication<FrontendUser, FrontendUserProvider>();
        services.AddPermissionAuthorization();
        services
            .AddScoped<AuthenticatedActor>()
            .AddHttpContextAccessor();

        return services;
    }
}
