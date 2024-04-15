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

using System.IdentityModel.Tokens.Jwt;
using Energinet.DataHub.EDI.B2BApi;
using Energinet.DataHub.EDI.B2BApi.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

var runtime = RuntimeEnvironment.Default;
Energinet.DataHub.EDI.B2BApi.Program.TokenValidationParameters =
    runtime.AZURE_FUNCTIONS_ENVIRONMENT == "Development"
        ? Energinet.DataHub.EDI.B2BApi.Program.TokenValidationParameters
        : await TokenConfiguration.GetTokenValidationParametersAsync(runtime).ConfigureAwait(false);

using var host = HostFactory.CreateHost(Energinet.DataHub.EDI.B2BApi.Program.TokenValidationParameters);

await host.RunAsync().ConfigureAwait(false);

namespace Energinet.DataHub.EDI.B2BApi
{
    public static partial class Program
    {
        public static TokenValidationParameters TokenValidationParameters { get; set; } =
#pragma warning disable CA5404 // Do not disable token validation checks
            new()
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
                SignatureValidator = (token, parameters) => new JwtSecurityToken(token),
            };
#pragma warning restore CA5404 // Do not disable token validation checks
    }
}
