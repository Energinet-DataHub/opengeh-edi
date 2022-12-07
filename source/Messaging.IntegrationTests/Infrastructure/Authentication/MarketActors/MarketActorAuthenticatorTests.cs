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
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Messaging.Application.Configuration.Authentication;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.Actors;
using Messaging.Infrastructure.Configuration.Authentication;
using Messaging.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Messaging.IntegrationTests.Infrastructure.Authentication.MarketActors
{
    [IntegrationTest]
    public class MarketActorAuthenticatorTests : TestBase
    {
        private readonly MarketActorAuthenticator _authenticator;

        public MarketActorAuthenticatorTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _authenticator = new MarketActorAuthenticator(GetService<IDbConnectionFactory>());
        }

        [Fact]
        public void Current_user_is_not_authenticated()
        {
            Assert.IsType<NotAuthenticated>(_authenticator.CurrentIdentity);
        }

        [Fact]
        public async Task Can_not_authenticate_if_claims_principal_does_not_contain_expected_claims()
        {
            var claims = new List<Claim>()
            {
                new(ClaimTypes.Role, "balanceresponsibleparty"),
                new(ClaimTypes.Role, "electricalsupplier"),
            };
            var claimsPrincipal = CreateIdentity(claims);

            await _authenticator.AuthenticateAsync(claimsPrincipal);

            Assert.IsType<NotAuthenticated>(_authenticator.CurrentIdentity);
        }

        [Fact]
        public async Task Current_user_is_authenticated()
        {
            var claims = new List<Claim>()
            {
                new("azp", Guid.NewGuid().ToString()),
                new("actorid", "1234567890123"),
                new("actoridtype", "GLN"),
                new(ClaimTypes.Role, "electricalsupplier"),
                new(ClaimTypes.Role, "balanceresponsibleparty"),
            };
            var claimsPrincipal = CreateIdentity(claims);

            await _authenticator.AuthenticateAsync(claimsPrincipal);

            Assert.IsType<Authenticated>(_authenticator.CurrentIdentity);
            Assert.Equal(GetClaimValue(claimsPrincipal, "azp"), _authenticator.CurrentIdentity.Id);
            Assert.Equal(GetClaimValue(claimsPrincipal, "actorid"), _authenticator.CurrentIdentity.Number.Value);
            Assert.Equal(MarketRole.EnergySupplier, _authenticator.CurrentIdentity.Role);
            Assert.True(_authenticator.CurrentIdentity.HasRole("balanceresponsibleparty"));
            Assert.True(_authenticator.CurrentIdentity.HasRole("electricalsupplier"));
        }

        private static string? GetClaimValue(ClaimsPrincipal claimsPrincipal, string claimName)
        {
            return claimsPrincipal.FindFirst(claim => claim.Type.Equals(claimName, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private static ClaimsPrincipal CreateIdentity(List<Claim>? claims = null)
        {
            var validClaims = new List<Claim>()
            {
                new("azp", Guid.NewGuid().ToString()),
                new("actorid", Guid.NewGuid().ToString()),
                new("actoridtype", "GLN"),
                new(ClaimTypes.Role, "balanceresponsibleparty"),
                new(ClaimTypes.Role, "electricalsupplier"),
            };

            var identity = new ClaimsIdentity(claims ?? validClaims);
            return new ClaimsPrincipal(identity);
        }
    }
}
