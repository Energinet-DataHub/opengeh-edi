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
using System.Net.Http;
using System.Net.Http.Headers;
using B2B.Transactions.Api.Middleware.Authentication.Bearer;
using B2B.Transactions.Infrastructure.Authentication;
using B2B.Transactions.Infrastructure.Authentication.Errors;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Categories;

namespace B2B.Transactions.IntegrationTests.Infrastructure.Authentication.MarketActors
{
    [IntegrationTest]
    public class JwtTokenParserTests
    {
#pragma warning disable CA5404 // Do not disable token validation checks
        private static TokenValidationParameters DisableAllTokenValidations => new()
        {
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuer = false,
            SignatureValidator = (token, parameters) => new JwtSecurityToken(token),
        };
#pragma warning restore CA5404 // Do not disable token validation checks

        [Fact]
        public void Returns_failure_when_token_validation_fails()
        {
            var token = CreateToken();
            using var httpRequest = CreateRequestWithAuthorizationHeader("bearer " + token);

            var result = Parse(httpRequest, new TokenValidationParameters()
            {
                ValidateLifetime = true,
            });

            Assert.False(result.Success);
            Assert.IsType<TokenValidationFailed>(result.Error);
            Assert.Equal(token, result.Token);
            Assert.Null(result.ClaimsPrincipal);
        }

        [Fact]
        public void Returns_claims_principal()
        {
            using var httpRequest = CreateRequestWithAuthorizationHeader("bearer " + CreateToken());

            var result = Parse(httpRequest);

            Assert.True(result.Success);
            Assert.NotNull(result.ClaimsPrincipal);
        }

        [Fact]
        public void Returns_failure_when_no_authorization_header_is_set()
        {
            using var httpRequest = CreateRequest();

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType<NoAuthenticationHeaderSet>(result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        [Fact]
        public void Returns_failure_when_authorization_header_is_empty()
        {
            using var httpRequest = CreateRequestWithAuthorizationHeader("bearer ");

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType<AuthenticationHeaderIsNotBearerToken>(result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        [Fact]
        public void Authorization_header_must_start_with_bearer()
        {
            using var httpRequest = CreateRequestWithAuthorizationHeader("Nobearer " + CreateToken());

            var result = Parse(httpRequest);

            Assert.False(result.Success);
            Assert.IsType<AuthenticationHeaderIsNotBearerToken>(result.Error);
            Assert.Null(result.ClaimsPrincipal);
        }

        private static Result Parse(HttpRequestMessage httpRequest, TokenValidationParameters? validationParameters = null)
        {
            var principalParser = new JwtTokenParser(validationParameters ?? DisableAllTokenValidations);
            return principalParser.ParseFrom(httpRequest.Headers);
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage();
        }

        private static HttpRequestMessage CreateRequestWithAuthorizationHeader(string value)
        {
            var httpRequest = CreateRequest();
            httpRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(value);
            return httpRequest;
        }

        private static string CreateToken()
        {
            var token = new JwtBuilder()
                .Audience("2FCA35F1-8A86-4D85-9D17-A03BF2BB0431")
                .Subject("DB027BC7-EA56-4819-A614-56C46F91D9C2")
                .IssuedAt(DateTime.UtcNow)
                .Expires(DateTime.UtcNow.AddHours(1))
                .Issuer("https://www.issuersite.com")
                .CreateToken();
            return token;
        }
    }
}
