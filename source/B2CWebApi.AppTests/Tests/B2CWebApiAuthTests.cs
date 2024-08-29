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

using System.Net;
using System.Security.Claims;
using Energinet.DataHub.EDI.B2CWebApi.AppTests.Fixture;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2CWebApi.AppTests.Tests;

[Collection(nameof(B2CWebApiCollectionFixture))]
public class B2CWebApiAuthTests : IAsyncLifetime
{
    private readonly B2CWebApiFixture _fixture;

    public B2CWebApiAuthTests(B2CWebApiFixture fixture, ITestOutputHelper logger)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(logger);
    }

    private Claim[] RequiredActorClaims =>
    [
        new("actornumber", "1234567890123"),
        new("marketroles", ActorRole.EnergySupplier.Name),
    ];

    private string[] RequiredRoles => [
        "request-wholesale-settlement:view",
    ];

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task RequestWholesaleSettlement_WhenNoToken_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        using var request = B2CWebApiRequests.CreateRequestWholesaleSettlementRequest();

        // => Use no token
        request.Headers.Authorization = null;

        // Act
        var response = await _fixture.WebApiClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestWholesaleSettlement_WhenFakeToken_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        using var request = B2CWebApiRequests.CreateRequestWholesaleSettlementRequest();

        // => Use a fake token with the required roles and claims
        request.Headers.Authorization = _fixture.OpenIdJwtManager.JwtProvider
            .CreateFakeTokenAuthenticationHeader(
                roles: RequiredRoles,
                extraClaims: RequiredActorClaims);

        // Act
        var response = await _fixture.WebApiClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestWholesaleSettlement_WhenValidTokenButMissingRole_ReturnsForbiddenStatusCode()
    {
        // Arrange
        using var request = B2CWebApiRequests.CreateRequestWholesaleSettlementRequest();

        // => Use a valid token with the required claims but missing the required roles
        request.Headers.Authorization = await _fixture.OpenIdJwtManager.JwtProvider
            .CreateInternalTokenAuthenticationHeaderAsync(
                roles: [],
                extraClaims: RequiredActorClaims);

        // Act
        var response = await _fixture.WebApiClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RequestWholesaleSettlement_WhenValidTokenWithRequiredRole_ReturnsOkStatusCode()
    {
        // Arrange
        using var request = B2CWebApiRequests.CreateRequestWholesaleSettlementRequest();

        // => Use a valid token with the required roles and claims
        request.Headers.Authorization = await _fixture.OpenIdJwtManager.JwtProvider
            .CreateInternalTokenAuthenticationHeaderAsync(
                roles: RequiredRoles,
                extraClaims: RequiredActorClaims);

        // Act
        var response = await _fixture.WebApiClient.SendAsync(request);

        // Assert
        using var assertionScope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ensureSuccess = () => response.EnsureSuccessStatusCodeWithLogAsync(_fixture.TestLogger);
        await ensureSuccess.Should().NotThrowAsync();
    }
}
