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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Api.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors
{
    [IntegrationTest]
    public class MarketActorAuthenticatorTests : TestBase
    {
        private readonly IMarketActorAuthenticator _authenticator;
        private readonly AuthenticatedActor _authenticatedActor;

        public MarketActorAuthenticatorTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
            : base(integrationTestFixture, testOutputHelper)
        {
            _authenticator = GetService<IMarketActorAuthenticator>();
            _authenticatedActor = GetService<AuthenticatedActor>();

            var authenticatedActor = GetService<AuthenticatedActor>();
            authenticatedActor.SetAuthenticatedActor(null!);
        }

        [Fact]
        public void Current_user_is_not_authenticated()
        {
            Assert.Throws<InvalidOperationException>(() => _authenticatedActor.CurrentActorIdentity);
        }

        [Fact]
        public async Task Cannot_authenticate_when_user_has_no_roles()
        {
            await CreateActorAsync();
            var claims = new List<Claim>()
            {
                new(ClaimsMap.UserId, SampleData.StsAssignedUserId),
            };
            var claimsPrincipal = CreateIdentity(claims);

            var authenticated = await _authenticator.AuthenticateAsync(claimsPrincipal, CancellationToken.None);

            Assert.False(authenticated);
        }

        [Fact]
        public async Task Can_not_authenticate_if_claims_principal_does_not_contain_user_id_claim()
        {
            var claims = new List<Claim>()
            {
                ClaimsMap.RoleFrom(ActorRole.EnergySupplier),
                ClaimsMap.RoleFrom(ActorRole.GridOperator),
            };
            var claimsPrincipal = CreateIdentity(claims);

            var authenticated = await _authenticator.AuthenticateAsync(claimsPrincipal, CancellationToken.None);

            Assert.False(authenticated);
        }

        [Fact]
        public async Task Current_user_is_authenticated()
        {
            await CreateActorAsync();
            var claims = new List<Claim>()
            {
                new(ClaimsMap.UserId, SampleData.StsAssignedUserId),
                ClaimsMap.RoleFrom(ActorRole.EnergySupplier),
            };
            var claimsPrincipal = CreateIdentity(claims);

            var authenticated = await _authenticator.AuthenticateAsync(claimsPrincipal, CancellationToken.None);

            Assert.True(authenticated);
            Assert.Equal(_authenticatedActor.CurrentActorIdentity.ActorNumber.Value, SampleData.ActorNumber);
            Assert.True(_authenticatedActor.CurrentActorIdentity.HasRole(ActorRole.EnergySupplier));
        }

        private static ClaimsPrincipal CreateIdentity(List<Claim>? claims = null)
        {
            var validClaims = new List<Claim>()
            {
                new(ClaimsMap.UserId, Guid.NewGuid().ToString()),
                ClaimsMap.RoleFrom(ActorRole.GridOperator),
                ClaimsMap.RoleFrom(ActorRole.EnergySupplier),
            };

            var identity = new ClaimsIdentity(claims ?? validClaims);
            return new ClaimsPrincipal(identity);
        }

        private Task CreateActorAsync()
        {
            return CreateActorIfNotExistAsync(new CreateActorDto(
                SampleData.StsAssignedUserId,
                ActorNumber.Create(SampleData.ActorNumber)));
        }
    }
}
