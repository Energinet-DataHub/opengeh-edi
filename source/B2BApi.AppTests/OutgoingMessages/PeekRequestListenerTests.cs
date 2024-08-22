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
using System.Net.Http.Headers;
using System.Text;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.Authentication;
using Energinet.DataHub.EDI.B2BApi.OutgoingMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.OutgoingMessages;

/// <summary>
/// Tests verifying the configuration and behaviour of the <see cref="PeekRequestListener"/> http endpoint.
/// </summary>
[Collection(nameof(B2BApiAppCollectionFixture))]
public class PeekRequestListenerTests : IAsyncLifetime
{
    public PeekRequestListenerTests(B2BApiAppFixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.SetTestOutputHelper(testOutputHelper);
    }

    private B2BApiAppFixture Fixture { get; }

    public Task InitializeAsync()
    {
        Fixture.AppHostManager.ClearHostLog();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Fixture.SetTestOutputHelper(null!);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Given_PersistedActorAndNoQeueue_When_CallingPeekAggregationsWithValidContentTypeAndBearerToken_Then_ResponseShouldBeNoContent()
    {
        var messageCategory = "aggregations";

        // The actor must exist in the database
        var actorNumber = ActorNumber.Create("1234567890123");
        var externalId = Guid.NewGuid().ToString();
        await Fixture.DatabaseManager.AddActorAsync(actorNumber, externalId);

        // The bearer token must contain:
        //  * the actor role matching any valid/known role in the ClaimsMap
        //  * the external id matching the actor in the database
        var actorRole = ActorRole.MeteredDataResponsible;
        var b2bToken = new JwtBuilder()
            .WithRole(ClaimsMap.RoleFrom(actorRole).Value)
            .WithClaim(ClaimsMap.UserId, externalId)
            .CreateToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/peek/{messageCategory}");
        request.Content = new StringContent(
            string.Empty,
            Encoding.UTF8,
            "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", b2bToken);

        // Act
        using var actualResponse = await Fixture.AppHostManager.HttpClient.SendAsync(request);

        // Assert
        actualResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
