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
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.SubsystemHttpTrigger;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueHttpEndpointTests : IAsyncLifetime
{
    public EnqueueHttpEndpointTests(
        B2BApiAppFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.SetTestOutputHelper(testOutputHelper);
    }

    private B2BApiAppFixture Fixture { get; }

    public async Task InitializeAsync()
    {
        Fixture.AppHostManager.ClearHostLog();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Fixture.SetTestOutputHelper(null!);
        await Task.CompletedTask;
    }

    [Fact(Skip = "Need a trigger to run this test")]
    public async Task Given_SubsystemRequestWithValidToken_When_Requesting_Then_SuccessfulRequest()
    {
        // Arrange
        var token = Fixture.CreateSubsystemToken();

        var httpRequest = CreateHttpRequest(token);
        using var httpResponse = await Fixture.AppHostManager.HttpClient.SendAsync(httpRequest);

        // Assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Skip = "Need a trigger to run this test")]
    public async Task Given_SubsystemRequestWithInvalidToken_When_Requesting_Then_RejectedRequest()
    {
        // Arrange
        var token = "InvalidToken";

        var httpRequest = CreateHttpRequest(token);

        using var httpResponse = await Fixture.AppHostManager.HttpClient.SendAsync(httpRequest);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "Need a trigger to run this test")]
    public async Task Given_RequestWithActorToken_When_Requesting_Then_RejectedRequest()
    {
        var actorClientId = Guid.NewGuid().ToString();
        var actorNumber = ActorNumber.Create("5790000392551");

        // Arrange
        await Fixture.DatabaseManager.AddActorAsync(actorNumber, actorClientId);
        // Arrange
        var token = new JwtBuilder()
            .WithRole(ClaimsMap.RoleFrom(ActorRole.EnergySupplier).Value)
            .WithClaim(ClaimsMap.ActorClientId, actorClientId)
            .CreateToken();

        var httpRequest = CreateHttpRequest(token);

        using var httpResponse = await Fixture.AppHostManager.HttpClient.SendAsync(httpRequest);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpRequestMessage CreateHttpRequest(string token)
    {
        // TODO: Update path when we have a trigger to enqueue with
        var request = new HttpRequestMessage(HttpMethod.Post, "api/incomingMessages/notifyvalidatedmeasuredata");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
