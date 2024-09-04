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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.AuditLog.AuditLogClient;

public static class AuditLogClientDependencyInjectionExtensions
{
    /// <summary>
    /// Register the <see cref="IAuditLogClient"/>, which requires an <see cref="AuditLogOptions"/> section to be present in the <see cref="IConfiguration"/>.
    /// <remarks>See <see cref="AuditLogOptions"/> for information about the required <see cref="IConfiguration"/> section.</remarks>
    /// </summary>
    public static IServiceCollection AddAuditLogClient(this IServiceCollection services)
    {
        services.AddOptions<AuditLogOptions>()
            .BindConfiguration(AuditLogOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddScoped<IAuditLogClient, AuditLogHttpClient>();

        return services;
    }
}
