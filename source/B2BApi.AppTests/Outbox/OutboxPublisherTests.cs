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

using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.Outbox.Domain;
using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.Functions;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Outbox;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class OutboxPublisherTests : IAsyncLifetime
{
    public OutboxPublisherTests(B2BApiAppFixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.SetTestOutputHelper(testOutputHelper);
    }

    private B2BApiAppFixture Fixture { get; }

    public async Task InitializeAsync()
    {
        Fixture.AuditLogMockServer.ResetCallLogs();

        await using var dbContext = Fixture.DatabaseManager.CreateDbContext();
        await dbContext.Database.ExecuteSqlAsync($"TRUNCATE TABLE [dbo].[Outbox]");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Given_AuditLogOutboxMessage_When_RunningOutboxPublisher_Then_CorrectAuditLogRequestSent()
    {
        var now = Instant.FromUtc(2024, 09, 05, 13, 37);

        // Arrange
        var expectedLogId = Guid.NewGuid();
        var auditLogOutboxMessageV1 = new AuditLogOutboxMessageV1(
            new Serializer(),
            new AuditLogOutboxMessageV1Payload(
                LogId: expectedLogId,
                UserId: Guid.NewGuid(),
                ActorId: Guid.NewGuid(),
                ActorNumber: null,
                MarketRoles: null,
                SystemId: Guid.NewGuid(),
                Permissions: "the-permissions",
                OccuredOn: now,
                Activity: "an-activity",
                Origin: "an-origin",
                Payload: "a-payload",
                AffectedEntityType: "an-entity-type",
                AffectedEntityKey: "an-entity-key"));

        var outboxMessage = new OutboxMessage(
            now,
            auditLogOutboxMessageV1.Type,
            await auditLogOutboxMessageV1.SerializeAsync());

        await using (var writeOutboxContext = Fixture.DatabaseManager.CreateDbContext<OutboxContext>())
        {
            writeOutboxContext.Outbox.Add(outboxMessage);
            await writeOutboxContext.SaveChangesAsync();
        }

        // Act
        await Fixture.AppHostManager.TriggerFunctionAsync(nameof(OutboxPublisher));

        // Assert
        await using var readOutboxContext = Fixture.DatabaseManager.CreateDbContext<OutboxContext>();
        var (success, actualOutboxMessage) = await readOutboxContext.WaitForOutboxMessageToBeProcessedAsync(outboxMessage.Id);

        actualOutboxMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();
        success.Should().BeTrue();
        actualOutboxMessage!.PublishedAt.Should().NotBeNull();
        actualOutboxMessage.FailedAt.Should().BeNull();

        var auditLogIngestionCalls = Fixture.AuditLogMockServer.GetAuditLogIngestionCalls();

        var auditLogRequest = auditLogIngestionCalls
            .Should().ContainSingle()
            .Subject
            .Request;

        var jsonBody = JToken.Parse(auditLogRequest.Body ?? string.Empty);
        jsonBody.Value<string>("LogId").Should().Be(expectedLogId.ToString());
    }
}
