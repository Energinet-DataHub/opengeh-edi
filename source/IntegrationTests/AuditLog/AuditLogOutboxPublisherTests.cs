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
using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.AuditLog;
using Energinet.DataHub.EDI.AuditLog.AuditLogClient;
using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.AuditLog.Fixture;
using Energinet.DataHub.EDI.Outbox.Interfaces;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.AuditLog;

public class AuditLogOutboxPublisherTests : IClassFixture<AuditLogTestFixture>, IAsyncLifetime
{
    /// <summary>
    /// Serialized instance of <see cref="AuditLogOutboxMessageV1Payload"/>
    /// </summary>
    private const string AuditLogOutboxMessageV1PayloadJson = @"{
        ""LogId"":""dab87943-9885-4015-90f1-3709ace8ffd3"",
        ""UserId"":""21fb8ca5-0edb-464e-88a2-8bbabc0bbf1f"",
        ""ActorId"":""6e64c193-bb0c-4fe0-9eb3-728c8fc90f9e"",
        ""SystemId"":""9415d80c-7af4-435e-a3b8-9c679783149e"",
        ""Permissions"":""expected-permissions"",
        ""OccuredOn"":""2024-09-05T13:37:00Z"",
        ""Activity"":""expected-activity"",
        ""Origin"":""expected-origin"",
        ""Payload"":""expected-payload"",
        ""AffectedEntityType"":""expected-entity-type"",
        ""AffectedEntityKey"":""expected-entity-key""
    }";

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
    public async Task Given_AuditLogOutboxMessageV1PayloadJson_When_PublishAuditLogOutboxMessage_Then_MessageIsCorrectlyPublished()
    {
        // Arrange
        var serviceProvider = ServiceCollection.BuildServiceProvider();

        var serializer = serviceProvider.GetRequiredService<ISerializer>();
        var auditLogOutboxPublisher = serviceProvider.GetRequiredService<AuditLogOutboxPublisher>();

        // => Expected values taken from AuditLogOutboxMessageV1PayloadJson
        var expectedLogId = Guid.Parse("dab87943-9885-4015-90f1-3709ace8ffd3");
        var expectedUserId = Guid.Parse("21fb8ca5-0edb-464e-88a2-8bbabc0bbf1f");
        var expectedActorId = Guid.Parse("6e64c193-bb0c-4fe0-9eb3-728c8fc90f9e");
        var expectedSystemId = Guid.Parse("9415d80c-7af4-435e-a3b8-9c679783149e");
        var expectedPermissions = "expected-permissions";
        var expectedOccuredOn = "2024-09-05T13:37:00Z";
        var expectedActivity = "expected-activity";
        var expectedOrigin = "expected-origin";
        var expectedPayload = "expected-payload";
        var expectedEntityType = "expected-entity-type";
        var expectedEntityKey = "expected-entity-key";

        // Act
        // => Publish the AuditLogOutboxMessageV1PayloadJson using expected serialized payload, to ensure
        // the AuditLogOutboxPublisher can (still) deserialize the payload
        await auditLogOutboxPublisher.PublishAsync(AuditLogOutboxMessageV1PayloadJson);

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
        deserializedBody!.LogId.Should().Be(expectedLogId);
        deserializedBody.UserId.Should().Be(expectedUserId);
        deserializedBody.ActorId.Should().Be(expectedActorId);
        deserializedBody.SystemId.Should().Be(expectedSystemId); // EDI subsystem id
        deserializedBody.Permissions.Should().Be(expectedPermissions);
        deserializedBody.OccurredOn.Should().Be(expectedOccuredOn);
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
        var actualResult = auditLogOutboxPublisher.CanPublish("AuditLogOutboxMessageV1");

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
                    { "RevisionLogOptions:ApiAddress", Fixture.AuditLogMockServer.IngestionUrl },
                })
            .Build();

        ServiceCollection
            .AddSingleton<IConfiguration>(config)
            .AddHttpClient()
            .AddJavaScriptEncoder()
            .AddSerializer()
            .AddAuditLogOutboxPublisher(config)
            .AddTransient<AuditLogOutboxPublisher>(sp => (AuditLogOutboxPublisher)sp.GetRequiredService<IOutboxPublisher>());
    }
}
