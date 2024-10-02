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

using System.Dynamic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.Authentication;
using Energinet.DataHub.EDI.B2BApi.IncomingMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using NodaTime;
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

    public async Task InitializeAsync()
    {
        Fixture.AppHostManager.ClearHostLog();
        await using var context = Fixture.DatabaseManager.CreateDbContext<OutboxContext>();
        await context.Outbox.ExecuteDeleteAsync();
    }

    public Task DisposeAsync()
    {
        Fixture.SetTestOutputHelper(null!);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Given_PersistedActor_When_CallingIncomingMessagesWithValidDocumentAndBearerToken_Then_ResponseShouldBeAccepted()
    {
        using var request = await CreateHttpRequest(
            "TestData/Messages/json/RequestAggregatedMeasureData.json",
            IncomingDocumentType.RequestAggregatedMeasureData.Name,
            "application/json");

        // Act
        using var actualResponse = await Fixture.AppHostManager.HttpClient.SendAsync(request);

        // Assert
        var contentType = actualResponse.Content.Headers.ContentType;
        contentType.Should().NotBeNull();
        contentType!.MediaType.Should().Be("application/json");
        contentType.CharSet.Should().Be("utf-8");
        var content = await actualResponse.Content.ReadAsByteArrayAsync();
        Encoding.UTF8.GetString(content).Should().BeEmpty();
        actualResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task
        Given_PersistedActor_When_CallingIncomingMessagesWithInvalidDocumentAndBearerToken_Then_ResponseShouldBeBadRequest()
    {
        using var request = await CreateHttpRequest(
            "TestData/Messages/xml/RequestWholesaleSettlement.xml",
            IncomingDocumentType.RequestWholesaleSettlement.Name,
            "application/xml");

        // Act
        using var actualResponse = await Fixture.AppHostManager.HttpClient.SendAsync(request);

        // Assert
        var contentType = actualResponse.Content.Headers.ContentType;
        contentType.Should().NotBeNull();
        contentType!.MediaType.Should().Be("application/xml");
        contentType.CharSet.Should().Be("utf-8");
        var content = await actualResponse.Content.ReadAsByteArrayAsync();
        Encoding.UTF8.GetString(content).Should().Contain("receiver_MarketParticipant.marketRole");
        actualResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Given_PersistedActor_When_CallingIncomingMessagesWithValidDocumentAndBearerToken_Then_CorrectAuditLogRequestAddedToOutbox()
    {
        // Arrange
        var serializer = new Serializer();
        var documentTypeName = IncomingDocumentType.RequestAggregatedMeasureData.Name;
        var actorNumber = ActorNumber.Create("5790000392551");
        var actorRole = ActorRole.EnergySupplier;
        var jsonDocument = await File.ReadAllTextAsync("TestData/Messages/json/RequestAggregatedMeasureData.json");

        // The actor must exist in the database
        var externalId = Guid.NewGuid().ToString();
        await Fixture.DatabaseManager.AddActorAsync(actorNumber, externalId);

        // The bearer token must contain:
        //  * the actor role matching the document content
        //  * the external id matching the actor in the database
        var b2bToken = new JwtBuilder()
            .WithRole(ClaimsMap.RoleFrom(actorRole).Value)
            .WithClaim(ClaimsMap.ActorId, externalId)
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

        await using var outboxContext = Fixture.DatabaseManager.CreateDbContext<OutboxContext>();
        var outboxMessage = outboxContext.Outbox.SingleOrDefault();
        outboxMessage!.Type.Should().Be(AuditLogOutboxMessageV1.OutboxMessageType);
        outboxMessage.ShouldProcessNow(SystemClock.Instance).Should().BeTrue();
        var auditLogPayload = serializer.Deserialize<AuditLogOutboxMessageV1Payload>(outboxMessage.Payload);

        using var assertionScope = new AssertionScope();
        auditLogPayload.LogId.Should().NotBeEmpty();
        auditLogPayload.UserId.Should().Be(Guid.Empty);
        auditLogPayload.ActorId.Should().Be(Guid.Empty);
        auditLogPayload.ActorNumber.Should().Be(actorNumber.Value);
        auditLogPayload.MarketRoles.Should().Be(actorRole.Name);
        auditLogPayload.SystemId.Should().Be(Guid.Parse("688b2dca-7231-490f-a731-d7869d33fe5e")); // EDI subsystem id
        auditLogPayload.Permissions.Should().Be(actorRole.Name);
        auditLogPayload.OccuredOn.Should().NotBeNull();
        auditLogPayload.Activity.Should().Be(AuditLogActivity.RequestCalculationResults.Identifier);
        auditLogPayload.Origin.Should().Be(request.RequestUri?.AbsoluteUri);
        auditLogPayload.AffectedEntityType.Should().NotBeNullOrWhiteSpace();
        auditLogPayload.Payload.Should().NotBeNull();
        dynamic payload = serializer.Deserialize<ExpandoObject>(auditLogPayload.Payload!.ToString()!);
        ((string)payload.IncomingDocumentType).Should().Be(documentTypeName);
        ((string)payload.Message).Should().NotBeNull();
    }

    private async Task<HttpRequestMessage> CreateHttpRequest(string filePath, string documentType, string contentType)
    {
        HttpRequestMessage? request = null;
        try
        {
            // The following must match with the JSON/XML document content
            var actorNumber = ActorNumber.Create("5790000392551");
            var actorRole = ActorRole.EnergySupplier;
            var document = await File.ReadAllTextAsync(filePath);
            document = document
                .Replace("{MessageId}", Guid.NewGuid().ToString())
                .Replace("{TransactionId}", Guid.NewGuid().ToString());

            // The actor must exist in the database
            var externalId = Guid.NewGuid().ToString();
            await Fixture.DatabaseManager.AddActorAsync(actorNumber, externalId);

            // The bearer token must contain:
            //  * the actor role matching the document content
            //  * the external id matching the actor in the database
            var b2bToken = new JwtBuilder()
                .WithRole(ClaimsMap.RoleFrom(actorRole).Value)
                .WithClaim(ClaimsMap.ActorId, externalId)
                .CreateToken();

            request = new HttpRequestMessage(HttpMethod.Post, $"api/incomingMessages/{documentType}")
            {
                Content = new StringContent(
                    document,
                    Encoding.UTF8,
                    contentType),
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", b2bToken);

            return request;
        }
        catch
        {
            request?.Dispose();
            throw;
        }
    }
}
