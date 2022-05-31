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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using Messaging.Infrastructure.Configuration.Authentication.Errors;
using Microsoft.IdentityModel.Tokens;

namespace Messaging.Infrastructure.Configuration.Authentication
{
    public class JwtTokenParser
    {
        private readonly TokenValidationParameters _validationParameters;

        public JwtTokenParser(TokenValidationParameters validationParameters)
        {
            _validationParameters = validationParameters ?? throw new ArgumentNullException(nameof(validationParameters));
        }

        public Result ParseFrom(HttpHeaders requestHeaders)
        {
            if (requestHeaders == null) throw new ArgumentNullException(nameof(requestHeaders));
            if (requestHeaders.TryGetValues("authorization", out var authorizationHeaderValues) == false)
            {
                return Result.Failed(new NoAuthenticationHeaderSet());
            }

            var authorizationHeaderValue = authorizationHeaderValues.FirstOrDefault();
            if (authorizationHeaderValue is null || IsBearer(authorizationHeaderValue) == false)
            {
                return Result.Failed(new AuthenticationHeaderIsNotBearerToken());
            }

            return ExtractPrincipalFrom(ParseBearerToken(authorizationHeaderValue));
        }

        private static string ParseBearerToken(string authorizationHeaderValue)
        {
            if (authorizationHeaderValue == null) throw new ArgumentNullException(nameof(authorizationHeaderValue));
            return authorizationHeaderValue.Substring(7);
        }

        private static bool IsBearer(string authorizationHeaderValue)
        {
            if (authorizationHeaderValue == null) throw new ArgumentNullException(nameof(authorizationHeaderValue));
            return authorizationHeaderValue.StartsWith("bearer", StringComparison.OrdinalIgnoreCase) && authorizationHeaderValue.Length > 7;
        }

        private Result ExtractPrincipalFrom(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, _validationParameters, out _);
                return Result.Succeeded(principal);
            }
            catch (SecurityTokenException e)
            {
                return Result.Failed(new TokenValidationFailed(e.Message), token);
            }
        }
    }
}
