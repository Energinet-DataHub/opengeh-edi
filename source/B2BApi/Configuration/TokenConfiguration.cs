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
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.EDI.B2BApi.Configuration;

public static class TokenConfiguration
{
    public static async Task<TokenValidationParameters> GetTokenValidationParametersAsync(RuntimeEnvironment runtime)
    {
        var tenantId = Environment.GetEnvironmentVariable("B2C_TENANT_ID")
                       ?? throw new InvalidOperationException("B2C tenant id not found.");

        var audience = Environment.GetEnvironmentVariable("BACKEND_SERVICE_APP_ID")
                       ?? throw new InvalidOperationException("Backend service app id not found.");

        var metaDataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
        var openIdConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metaDataAddress,
            new OpenIdConnectConfigurationRetriever());
        var stsConfig = await openIdConfigurationManager.GetConfigurationAsync().ConfigureAwait(false);
        return new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero,
            ValidAudience = audience,
            IssuerSigningKeys = stsConfig.SigningKeys,
            ValidIssuer = stsConfig.Issuer,
        };
    }
}
