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

using System.Security.Claims;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.Tests.B2CWebApi;

public class FrontendUserProviderTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public FrontendUserProviderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("EnergySupplier", "DDQ")]
    [InlineData("MeteredDataResponsible", "MDR")]
    [InlineData("BalanceResponsibleParty", "DDK")]
    [InlineData("GridAccessProvider", "DDM")]
    [InlineData("SystemOperator", "EZ")]
    [InlineData("Delegated", "DEL")]
    [InlineData("DataHubAdministrator", "")]
    public async Task Given_ValidFrontendUserRole_When_ProvideUserAsync_Then_CorrectAuthenticatedUserIsSet(string marketrole, string expectedCode)
    {
        // Arrange
        const string expectedActorNumber = "1234567890123";

        var authenticatedActor = new AuthenticatedActor();
        var logger = new NullLogger<FrontendUserProvider>();

        var sut = new FrontendUserProvider(logger, authenticatedActor);

        // Act
        await sut.ProvideUserAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            [
                new("actornumber", expectedActorNumber),
                new("marketroles", marketrole),
                new Claim("azp", "random-string"),
            ]);

        // Assert
        authenticatedActor
            .TryGetCurrentActorIdentity(out var authenticatedActorIdentity)
            .Should()
            .BeTrue("because the actor identity should be set");

        authenticatedActorIdentity
            .Should()
            .NotBeNull("because the actor identity should be set");

        authenticatedActorIdentity!.ActorRole
            .Should()
            .NotBeNull();

        authenticatedActorIdentity!.ActorRole!.Code
            .Should()
            .Be(expectedCode);

        authenticatedActorIdentity.ActorNumber.Value
            .Should()
            .Be(expectedActorNumber);

        authenticatedActorIdentity.Restriction
            .Should()
            .Be(Restriction.None);
    }

    [Fact]
    public async Task Given_InvalidFrontendUserRole_When_ProvideUserAsync_Then_Exception()
    {
        // Arrange
        var authenticatedActor = new AuthenticatedActor();
        var logger = new TestLogger<FrontendUserProvider>(_testOutputHelper);

        var sut = new FrontendUserProvider(logger, authenticatedActor);

        // Act
        var act = () => sut.ProvideUserAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            [
                new("actornumber", "1234567890123"),
                new("marketroles", "invalid-role-name"),
                new Claim("azp", "random-string"),
            ]);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        authenticatedActor
            .TryGetCurrentActorIdentity(out var authenticatedActorIdentity)
            .Should()
            .BeFalse("because the actor identity should not be set");

        authenticatedActorIdentity
            .Should()
            .BeNull("because the actor identity should not be set");
    }
}
