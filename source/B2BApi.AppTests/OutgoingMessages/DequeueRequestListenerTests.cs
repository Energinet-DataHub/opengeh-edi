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
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.Authentication;
using Energinet.DataHub.EDI.B2BApi.OutgoingMessages;
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

namespace Energinet.DataHub.EDI.B2BApi.AppTests.OutgoingMessages;

/// <summary>
/// Tests verifying the configuration and behaviour of the <see cref="DequeueRequestListener"/> http endpoint.
/// </summary>
[Collection(nameof(B2BApiAppCollectionFixture))]
public class DequeueRequestListenerTests : IAsyncLifetime
{
    public DequeueRequestListenerTests(B2BApiAppFixture fixture, ITestOutputHelper testOutputHelper)
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
    public async Task Given_PersistedActorAndNoQeueue_When_CallingDequeueWithUnknownMessageIdAndBearerToken_Then_ResponseShouldBeBadRequest()
    {
        var messageId = Guid.NewGuid().ToString();

        // The actor must exist in the database
        var actorNumber = ActorNumber.Create("1234567890123");
        var externalId = Guid.NewGuid().ToString();
        await Fixture.DatabaseManager.AddActorAsync(actorNumber, externalId);

        // The bearer token must contain:
        //  * the actor role matching any valid/known role in the ClaimsMap
        //  * the external id matching the actor in the database
        var actorRole = ActorRole.BalanceResponsibleParty;
        var b2bToken = new JwtBuilder()
            .WithRole(ClaimsMap.RoleFrom(actorRole).Value)
            .WithClaim(ClaimsMap.ActorId, externalId)
            .CreateToken();

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/dequeue/{messageId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", b2bToken);

        // Act
        using var actualResponse = await Fixture.AppHostManager.HttpClient.SendAsync(request);

        // Assert
        actualResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await actualResponse.Content.ReadAsStringAsync()).Should().BeEmpty();
        actualResponse.Content.Headers.ContentType.Should().NotBeNull();
        actualResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        actualResponse.Content.Headers.ContentType.CharSet.Should().Be("utf-8");
    }

    [Fact]
    public async Task Given_PersistedActorAndNoQeueue_When_CallingDequeueWithUnknownMessageIdAndBearerToken_Then_CorrectAuditLogRequestAddedToOutbox()
    {
        // Arrange
        var serializer = new Serializer();
        var messageId = Guid.NewGuid().ToString();

        // The actor must exist in the database
        var actorNumber = ActorNumber.Create("1234567890123");
        var externalId = Guid.NewGuid().ToString();
        await Fixture.DatabaseManager.AddActorAsync(actorNumber, externalId);

        // The bearer token must contain:
        //  * the actor role matching any valid/known role in the ClaimsMap
        //  * the external id matching the actor in the database
        var actorRole = ActorRole.BalanceResponsibleParty;
        var b2bToken = new JwtBuilder()
            .WithRole(ClaimsMap.RoleFrom(actorRole).Value)
            .WithClaim(ClaimsMap.ActorId, externalId)
            .CreateToken();

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/dequeue/{messageId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", b2bToken);

        // Act
        using var actualResponse = await Fixture.AppHostManager.HttpClient.SendAsync(request);

        // Assert
        actualResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

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
        auditLogPayload.Activity.Should().Be(AuditLogActivity.Dequeue.Identifier);
        auditLogPayload.Origin.Should().Be(request.RequestUri?.AbsoluteUri);
        auditLogPayload.Payload.Should().NotBeNull();
        auditLogPayload.AffectedEntityType.Should().NotBeNullOrWhiteSpace();
    }
}
