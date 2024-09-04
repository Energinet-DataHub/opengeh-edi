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
using System.Text.Json;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.AuditLog;
using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.AuditLog.AuditLogServerClient;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.AuditLog.Fixture;
using Energinet.DataHub.EDI.Outbox.Interfaces;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.AuditLog;

public class AuditLogOutboxPublisherTests : IClassFixture<AuditLogTestFixture>, IAsyncLifetime
{
    public AuditLogOutboxPublisherTests(AuditLogTestFixture fixture)
    {
        Fixture = fixture;
        SetupServiceCollection();
    }

    private ServiceCollection ServiceCollection { get; } = [];

    private AuditLogTestFixture Fixture { get; }

    public Task InitializeAsync()
    {
        Fixture.AuditLogMockServer.ResetCallLogs();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task When_PublishingAuditLogOutboxMessage_Then_MessageCorrectlyProcessed()
    {
        // Arrange
        var serviceProvider = ServiceCollection.BuildServiceProvider();

        var serializer = serviceProvider.GetRequiredService<ISerializer>();
        var auditLogOutboxPublisher = serviceProvider.GetRequiredService<AuditLogOutboxPublisher>();

        var expectedLogId = Guid.NewGuid();
        var expectedUserId = Guid.NewGuid();
        var expectedActorId = Guid.NewGuid();
        var expectedSystemId = Guid.NewGuid();
        var expectedPermissions = "expected-permissions";
        var expectedOccuredOn = Instant.FromUtc(2024, 09, 04, 13, 37);
        var expectedActivity = "expected-activity";
        var expectedOrigin = "expected-origin";
        var expectedPayload = "expected-payload";
        var expectedEntityType = "expected-entity-type";
        var expectedEntityKey = "expected-entity-key";
        var payload = new AuditLogPayload(
            LogId: expectedLogId,
            UserId: expectedUserId,
            ActorId: expectedActorId,
            SystemId: expectedSystemId,
            Permissions: expectedPermissions,
            OccuredOn: expectedOccuredOn,
            Activity: expectedActivity,
            Origin: expectedOrigin,
            Payload: expectedPayload,
            AffectedEntityType: expectedEntityType,
            AffectedEntityKey: expectedEntityKey);

        var serializedPayload = serializer.Serialize(payload);

        // Act
        await auditLogOutboxPublisher.PublishAsync(serializedPayload);

        // Assert
        var auditLogCalls = Fixture.AuditLogMockServer.GetAuditLogIngestionCalls();

        var auditLogCall = auditLogCalls.Should()
            .ContainSingle()
            .Subject;

        AuditLogRequestBody? deserializedBody;
        using (new AssertionScope())
        {
            // => Ensure that the audit log request was successful
            auditLogCall.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

            // => Ensure that the audit log request contains a body
            auditLogCall.Request.Body.Should().NotBeNull();

            // => Ensure that the audit log request body can be deserialized to an instance of AuditLogRequestBody
            var deserializeBody = () =>
                serializer.Deserialize<AuditLogRequestBody>(auditLogCall.Request.Body ?? string.Empty);

            deserializedBody = deserializeBody.Should().NotThrow().Subject;
            deserializedBody.Should().NotBeNull();
        }

        using var assertionScope = new AssertionScope();
        deserializedBody!.LogId.Should().NotBeEmpty();
        deserializedBody.UserId.Should().Be(expectedUserId);
        deserializedBody.ActorId.Should().Be(expectedActorId);
        deserializedBody.SystemId.Should().Be(expectedSystemId); // EDI subsystem id
        deserializedBody.Permissions.Should().Be(expectedPermissions);
        deserializedBody.OccurredOn.Should().Be(InstantPattern.General.Format(expectedOccuredOn));
        deserializedBody.Activity.Should().Be(expectedActivity);
        deserializedBody.Origin.Should().Be(expectedOrigin);
        deserializedBody.Payload.Should().Be(expectedPayload);
        deserializedBody.AffectedEntityType.Should().Be(expectedEntityType);
        deserializedBody.AffectedEntityKey.Should().Be(expectedEntityKey);
    }

    [Fact]
    public void When_AuditLogOutboxMessageV1_Then_MessageCanBeProcessedByPublisher()
    {
        // Arrange
        var serviceProvider = ServiceCollection.BuildServiceProvider();

        var auditLogOutboxPublisher = serviceProvider.GetRequiredService<AuditLogOutboxPublisher>();

        // Act
        var actualResult = auditLogOutboxPublisher.CanProcess("AuditLogOutboxMessageV1");

        // Assert
        actualResult.Should().BeTrue($"because {nameof(AuditLogOutboxPublisher)} should be able to process AuditLogOutboxMessageV1");
    }

    private void SetupServiceCollection()
    {
        var dbConnectionString = Fixture.DatabaseManager.ConnectionString;
        if (!dbConnectionString.Contains("Trust")) // Trust Server Certificate might be required for some
            dbConnectionString = $"{dbConnectionString};Trust Server Certificate=True;";

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "DB_CONNECTION_STRING", dbConnectionString },
                    { "AuditLog:IngestionUrl", Fixture.AuditLogMockServer.IngestionUrl },
                })
            .Build();

        ServiceCollection
            .AddSingleton<IConfiguration>(config)
            .AddHttpClient()
            .AddSerializer()
            .AddAuditLogOutboxPublisher()
            .AddTransient<AuditLogOutboxPublisher>(sp => (AuditLogOutboxPublisher)sp.GetRequiredService<IOutboxPublisher>());
    }
}
