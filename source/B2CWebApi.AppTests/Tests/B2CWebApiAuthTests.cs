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
using System.Net.Http.Json;
using Energinet.DataHub.EDI.B2CWebApi.AppTests.Fixture;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2CWebApi.AppTests.Tests;

[Collection(nameof(B2CWebApiCollectionFixture))]
public class B2CWebApiAuthTests : IAsyncLifetime
{
    private readonly B2CWebApiFixture _fixture;
    private readonly ITestOutputHelper _logger;

    public B2CWebApiAuthTests(B2CWebApiFixture fixture, ITestOutputHelper logger)
    {
        _fixture = fixture;
        _logger = logger;
    }

    private string[] RequiredRoles => ["request-wholesale-settlement:view"];

    public async Task InitializeAsync()
    {
        // Delete (if exists) and recreate the database to ensure a clean state
        await _fixture.DatabaseManager.CreateDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DatabaseManager.DeleteDatabaseAsync();
    }

    [Fact]
    public async Task RequestWholesaleSettlement_WhenNoToken_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        using var request = CreateWholesaleSettlementRequest();

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
        using var request = CreateWholesaleSettlementRequest();

        // => Use a fake token with the required roles and claims
        request.Headers.Authorization = _fixture.OpenIdJwtManager.JwtProvider
            .CreateFakeTokenAuthenticationHeader(
                roles: RequiredRoles,
                extraClaims: _fixture.RequiredActorClaims);

        // Act
        var response = await _fixture.WebApiClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestWholesaleSettlement_WhenValidTokenButMissingRole_ReturnsForbiddenStatusCode()
    {
        // Arrange
        using var request = CreateWholesaleSettlementRequest();

        // => Use a valid token with the required claims but missing the required roles
        request.Headers.Authorization = await _fixture.OpenIdJwtManager.JwtProvider
            .CreateInternalTokenAuthenticationHeaderAsync(
                roles: [],
                extraClaims: _fixture.RequiredActorClaims);

        // Act
        var response = await _fixture.WebApiClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RequestWholesaleSettlement_WhenValidTokenWithRequiredRole_ReturnsOkStatusCode()
    {
        // Arrange
        using var request = CreateWholesaleSettlementRequest();

        // => Use a valid token with the required roles and claims
        request.Headers.Authorization = await _fixture.OpenIdJwtManager.JwtProvider
            .CreateInternalTokenAuthenticationHeaderAsync(
                roles: RequiredRoles,
                extraClaims: _fixture.RequiredActorClaims);

        // Act
        var response = await _fixture.WebApiClient.SendAsync(request);

        // Assert
        using var assertionScope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ensureSuccess = () => response.EnsureSuccessStatusCodeWithLogAsync(_logger);
        await ensureSuccess.Should().NotThrowAsync();
    }

    private HttpRequestMessage CreateWholesaleSettlementRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/RequestWholesaleSettlement")
        {
            Content = JsonContent.Create(
                new RequestWholesaleSettlementMarketRequest(
                    CalculationType: CalculationType.WholesaleFixing,
                    StartDate: "2024-08-27T00:00:00Z",
                    EndDate: "2024-08-29T00:00:00Z",
                    GridArea: null,
                    EnergySupplierId: null,
                    Resolution: null,
                    PriceType: null)),
        };

        return request;
    }
}
