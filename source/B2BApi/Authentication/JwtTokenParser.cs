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
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.B2BApi.Authentication.Errors;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.EDI.B2BApi.Authentication
{
    public class JwtTokenParser
    {
        private readonly TokenValidationParameters _validationParameters;

        public JwtTokenParser(TokenValidationParameters validationParameters)
        {
            _validationParameters = validationParameters ?? throw new ArgumentNullException(nameof(validationParameters));
        }

        public Task<Result> ParseFromAsync(HttpHeaders requestHeaders)
        {
            ArgumentNullException.ThrowIfNull(requestHeaders);
            if (requestHeaders.TryGetValues("authorization", out var authorizationHeaderValues) == false)
            {
                return Task.FromResult(Result.Failed(new NoAuthenticationHeaderSet()));
            }

            var authorizationHeaderValue = authorizationHeaderValues.FirstOrDefault();
            if (authorizationHeaderValue is null || IsBearer(authorizationHeaderValue) == false)
            {
                return Task.FromResult(Result.Failed(new AuthenticationHeaderIsNotBearerToken()));
            }

            return ExtractPrincipalFromAsync(ParseBearerToken(authorizationHeaderValue));
        }

        private static string ParseBearerToken(string authorizationHeaderValue)
        {
            ArgumentNullException.ThrowIfNull(authorizationHeaderValue);
            return authorizationHeaderValue.Substring(7);
        }

        private static bool IsBearer(string authorizationHeaderValue)
        {
            ArgumentNullException.ThrowIfNull(authorizationHeaderValue);
            return authorizationHeaderValue.StartsWith("bearer", StringComparison.OrdinalIgnoreCase) && authorizationHeaderValue.Length > 7;
        }

        private async Task<Result> ExtractPrincipalFromAsync(string token)
        {
            try
            {
                var tokenHandler = new JsonWebTokenHandler();
                var tokenValidation = await tokenHandler.ValidateTokenAsync(token, _validationParameters).ConfigureAwait(false);
                if (tokenValidation.IsValid == false)
                {
                    return Result.Failed(new TokenValidationFailed("Token validation failed"), token);
                }

                var principal = new ClaimsPrincipal(tokenValidation.ClaimsIdentity);
                return Result.Succeeded(principal);
            }
            catch (ArgumentException e)
            {
                return Result.Failed(new TokenValidationFailed(e.Message), token);
            }
            catch (SecurityTokenException e)
            {
                return Result.Failed(new TokenValidationFailed(e.Message), token);
            }
        }
    }
}
