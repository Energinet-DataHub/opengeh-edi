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
using System.Net.Http.Json;
using Energinet.DataHub.EDI.B2CWebApi.AppTests.Fixture;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2CWebApi.AppTests.Tests;

[Collection(nameof(B2CWebApiCollectionFixture))]
public class B2CWebApiAuditLogTests : IAsyncLifetime
{
    private readonly B2CWebApiFixture _fixture;
    private readonly ITestOutputHelper _logger;

    public B2CWebApiAuditLogTests(B2CWebApiFixture fixture, ITestOutputHelper logger)
    {
        _fixture = fixture;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        // Delete (if exists) and recreate the database to ensure a clean state
        await _fixture.DatabaseManager.CreateDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DatabaseManager.DeleteDatabaseAsync();
    }

    [Fact]
    public async Task ArchivedMessageSearchRequest_WhenEndpointCalled_SendsAuditLogRequest()
    {
        // Arrange
        using var request = CreateArchivedMessageSearchRequest();
        request.Headers.Authorization = await _fixture
            .OpenIdJwtManager
            .JwtProvider
            .CreateInternalTokenAuthenticationHeaderAsync(extraClaims: _fixture.RequiredActorClaims);

        // Act
        var response = await _fixture.WebApiClient.SendAsync(request);

        // Assert
        await response.EnsureSuccessStatusCodeWithLogAsync(_logger);
        var auditLogCalls = _fixture.AuditLogMockServer.GetAuditLogIngestionCalls();

        auditLogCalls.Should().HaveCount(1);
        auditLogCalls.Single().Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }

    private HttpRequestMessage CreateArchivedMessageSearchRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/ArchivedMessageSearch")
        {
            Content = JsonContent.Create(
                new SearchArchivedMessagesCriteria(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null)),
        };
        return request;
    }
}
