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
using Energinet.DataHub.EDI.B2BApi.IncomingMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.IncomingMessages;

/// <summary>
/// Tests verifying the configuration and behaviour of the <see cref="IncomingMessageReceiver"/> http endpoint.
/// </summary>
[Collection(nameof(B2BApiAppCollectionFixture))]
public class IncomingMessageReceiverTests : IAsyncLifetime
{
    public IncomingMessageReceiverTests(B2BApiAppFixture fixture, ITestOutputHelper testOutputHelper)
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
    public async Task FunctionApp_WhenCallingIncomingMessages_ReturnOK()
    {
        var externalId = "external-id";
        // TODO: Insert actor in database
        var b2bToken = new JwtBuilder()
            .WithRole("energysupplier")
            .WithClaim(ClaimsMap.UserId, externalId)
            .CreateToken();

        // TODO: Help !!!
        var incomingDocumentTypeName = "not sure what this should be";
        var jsonDocument = "{id: \"1\"}";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/incomingMessages/{incomingDocumentTypeName}");
        request.Content = new StringContent(
            JsonConvert.SerializeObject(jsonDocument),
            Encoding.UTF8,
            "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", b2bToken);

        // Act
        using var actualResponse = await Fixture.AppHostManager.HttpClient.SendAsync(request);

        // Assert
        actualResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
