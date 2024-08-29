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
using Energinet.DataHub.EDI.AuditLog;
using Energinet.DataHub.EDI.AuditLog.AuditLogClient;
using Energinet.DataHub.EDI.B2CWebApi.AppTests.Fixture;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2CWebApi.AppTests.Tests;

[Collection(nameof(B2CWebApiCollectionFixture))]
public class B2CWebApiAuditLogTests : IAsyncLifetime
{
    private readonly B2CWebApiFixture _fixture;

    public B2CWebApiAuditLogTests(B2CWebApiFixture fixture, ITestOutputHelper logger)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(logger);
    }

    public static IEnumerable<object[]> GetB2CWebApiRequests()
    {
        return
        [
            [B2CWebApiRequests.CreateArchivedMessageGetDocumentRequest(), "DataHubAdministrator", AuditLogActivity.ArchivedMessagesGet],
            [B2CWebApiRequests.CreateArchivedMessageSearchRequest(), "DataHubAdministrator", AuditLogActivity.ArchivedMessagesSearch],
            [B2CWebApiRequests.CreateOrchestrationsRequest(), "DataHubAdministrator", AuditLogActivity.OrchestrationsSearch],
            [B2CWebApiRequests.CreateOrchestrationRequest(), "DataHubAdministrator", AuditLogActivity.OrchestrationsGet],
            [B2CWebApiRequests.CreateOrchestrationTerminateRequest(), "DataHubAdministrator", AuditLogActivity.OrchestrationsTerminate],
            [B2CWebApiRequests.CreateRequestAggregatedMeasureDataRequest(), ActorRole.EnergySupplier.Name, AuditLogActivity.RequestEnergyResults],
            [B2CWebApiRequests.CreateRequestWholesaleSettlementRequest(), ActorRole.EnergySupplier.Name, AuditLogActivity.RequestWholesaleResults],
        ];
    }

    public Task InitializeAsync()
    {
        _fixture.AuditLogMockServer.ResetCallLogs();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate that the correct audit log request is sent when a B2C Web API request is performed, according to
    /// the audit log documentation at: https://github.com/Energinet-DataHub/opengeh-revision-log/blob/main/docs/documentation-for-submitting-audit-logs.md
    /// </summary>
    [Theory(Skip = "Doesn't work because trying to reproduce error")]
    [MemberData(nameof(GetB2CWebApiRequests))]
    public async Task B2CWebApiRequest_WhenRequestPerformed_CorrectAuditLogRequestIsSent(
        HttpRequestMessage request,
        string actorRole,
        AuditLogActivity expectedActivity)
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var expectedActorId = Guid.NewGuid();
        string[] permissions =
        [
            "request-wholesale-settlement:view",
            "request-aggregated-measured-data:view",
            "calculations:manage",
        ];
        var expectedPermissions = string.Join(", ", permissions);

        request.Headers.Authorization = await _fixture
            .OpenIdJwtManager
            .JwtProvider
            .CreateInternalTokenAuthenticationHeaderAsync(
                userId: expectedUserId.ToString(),
                actorId: expectedActorId.ToString(),
                roles: permissions,
                extraClaims: [
                    new("actornumber", "1234567890123"),
                    new("marketroles", actorRole),
                ]);

        // Act
        await _fixture.WebApiClient.SendAsync(request);

        // Assert
        var auditLogCalls = _fixture.AuditLogMockServer.GetAuditLogIngestionCalls();

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
                JsonSerializer.Deserialize<AuditLogRequestBody>(auditLogCall.Request.Body ?? string.Empty);

            deserializedBody = deserializeBody.Should().NotThrow().Subject;
            deserializedBody.Should().NotBeNull();
        }

        deserializedBody!.LogId.Should().NotBeEmpty();
        deserializedBody.UserId.Should().Be(expectedUserId);
        deserializedBody.ActorId.Should().Be(expectedActorId);
        deserializedBody.SystemId.Should().Be(Guid.Parse("688b2dca-7231-490f-a731-d7869d33fe5e")); // EDI subsystem id
        deserializedBody.Permissions.Should().Be(expectedPermissions);
        InstantPattern.General.Parse(deserializedBody.OccurredOn).Success.Should().BeTrue($"because {deserializedBody.OccurredOn} should be a valid Instant");
        deserializedBody.Activity.Should().Be(expectedActivity.Identifier);
        deserializedBody.Origin.Should().Be(request.RequestUri?.AbsoluteUri);
        deserializedBody.Payload.Should().NotBeNull();
        deserializedBody.AffectedEntityType.Should().NotBeNullOrWhiteSpace();
        deserializedBody.AffectedEntityKey.Should().NotBeNull();

        request.Dispose();
    }
}
