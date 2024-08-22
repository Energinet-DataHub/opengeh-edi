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
    public async Task Given_PersistedActor_When_CallingIncomingMessagesWithValidDocumentAndBearerToken_Then_ResponseShouldBeAccepted()
    {
        // The following must match with the JSON document content
        var documentTypeName = IncomingDocumentType.RequestAggregatedMeasureData.Name;
        var actorNumber = ActorNumber.Create("5790000392551");
        var actorRole = ActorRole.EnergySupplier;
        var jsonDocument = await File.ReadAllTextAsync("TestData/Messages/json/RequestAggregatedMeasureData.json");

        // The actor must exist in the database
        var externalId = Guid.NewGuid().ToString();
        await using var sqlConnection = new Microsoft.Data.SqlClient.SqlConnection(Fixture.DatabaseManager.ConnectionString);
        {
            await using var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "INSERT INTO [dbo].[Actor] VALUES (@id, @actorNumber, @externalId)";
            sqlCommand.Parameters.AddWithValue("@id", Guid.NewGuid());
            sqlCommand.Parameters.AddWithValue("@actorNumber", actorNumber.Value);
            sqlCommand.Parameters.AddWithValue("@externalId", externalId);

            await sqlConnection.OpenAsync();
            await sqlCommand.ExecuteNonQueryAsync();
        }

        // The bearer token must contain:
        //  * the actor role matching the document content
        //  * the external id matching the actor in the database
        var b2bToken = new JwtBuilder()
            .WithRole(ClaimsMap.RoleFrom(actorRole).Value)
            .WithClaim(ClaimsMap.UserId, externalId)
            .CreateToken();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/incomingMessages/{documentTypeName}");
        request.Content = new StringContent(
            jsonDocument,
            Encoding.UTF8,
            "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", b2bToken);

        // Act
        using var actualResponse = await Fixture.AppHostManager.HttpClient.SendAsync(request);

        // Assert
        actualResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
