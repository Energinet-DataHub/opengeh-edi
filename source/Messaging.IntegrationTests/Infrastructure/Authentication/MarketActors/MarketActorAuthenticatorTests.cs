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
using Messaging.Application.Actors;
using Messaging.Application.Configuration.Authentication;
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
        private readonly IMarketActorAuthenticator _authenticator;

        public MarketActorAuthenticatorTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _authenticator = GetService<IMarketActorAuthenticator>();
        }

        [Fact]
        public void Current_user_is_not_authenticated()
        {
            Assert.IsType<NotAuthenticated>(_authenticator.CurrentIdentity);
        }

        [Fact]
        public async Task Can_not_authenticate_if_claims_principal_does_not_contain_user_id_claim()
        {
            var claims = new List<Claim>()
            {
                ClaimsMap.RoleFrom(MarketRole.EnergySupplier),
                ClaimsMap.RoleFrom(MarketRole.GridOperator),
            };
            var claimsPrincipal = CreateIdentity(claims);

            await _authenticator.AuthenticateAsync(claimsPrincipal);

            Assert.IsType<NotAuthenticated>(_authenticator.CurrentIdentity);
        }

        [Fact]
        public async Task Current_user_is_authenticated()
        {
            var createActorCommand =
                new CreateActor(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "1234567890123");
            await InvokeCommandAsync(createActorCommand).ConfigureAwait(false);
            var claims = new List<Claim>()
            {
                new(ClaimsMap.UserId, createActorCommand.B2CId),
                ClaimsMap.RoleFrom(MarketRole.EnergySupplier),
                ClaimsMap.RoleFrom(MarketRole.GridOperator),
            };
            var claimsPrincipal = CreateIdentity(claims);

            await _authenticator.AuthenticateAsync(claimsPrincipal);

            Assert.IsType<Authenticated>(_authenticator.CurrentIdentity);
            Assert.Equal(GetClaimValue(claimsPrincipal, ClaimsMap.UserId), _authenticator.CurrentIdentity.Id);
            Assert.Equal(MarketRole.EnergySupplier, _authenticator.CurrentIdentity.Role);
            Assert.Equal(_authenticator.CurrentIdentity.Number.Value, createActorCommand.IdentificationNumber);
            Assert.True(_authenticator.CurrentIdentity.HasRole(MarketRole.GridOperator.Name));
            Assert.True(_authenticator.CurrentIdentity.HasRole(MarketRole.EnergySupplier.Name));
        }

        private static string? GetClaimValue(ClaimsPrincipal claimsPrincipal, string claimName)
        {
            return claimsPrincipal.FindFirst(claim => claim.Type.Equals(claimName, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private static ClaimsPrincipal CreateIdentity(List<Claim>? claims = null)
        {
            var validClaims = new List<Claim>()
            {
                new(ClaimsMap.UserId, Guid.NewGuid().ToString()),
                ClaimsMap.RoleFrom(MarketRole.GridOperator),
                ClaimsMap.RoleFrom(MarketRole.EnergySupplier),
            };

            var identity = new ClaimsIdentity(claims ?? validClaims);
            return new ClaimsPrincipal(identity);
        }
    }
}
