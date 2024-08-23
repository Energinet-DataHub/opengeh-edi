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

using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.Core.App.FunctionApp;
using Energinet.DataHub.EDI.B2BApi.Authentication;
using Energinet.DataHub.EDI.B2BApi.Configuration.Middleware.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.B2BApi.Mocks;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.B2BApi;

[IntegrationTest]
public class WhenMarketActorAuthenticatorMiddlewareIsCalledTests : TestBase
{
    private readonly NextSpy _nextSpy;
    private readonly FunctionExecutionDelegate _next;
    private readonly FunctionContextBuilder _functionContextBuilder;

    public WhenMarketActorAuthenticatorMiddlewareIsCalledTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        AuthenticatedActor.SetAuthenticatedActor(null);
        _nextSpy = new NextSpy();
        _next = _nextSpy.Next;
        _functionContextBuilder = new FunctionContextBuilder(ServiceProvider);
    }

    [Fact]
    public async Task When_calling_authentication_middleware_with_certificate_then_next_is_called()
    {
        // Arrange
        using var testCertificate = CreateTestCertificate();
        await CreateActorCertificatesInDatabase(withThumbprint: testCertificate.Thumbprint);

        var functionContext = _functionContextBuilder
            .TriggeredByHttp("application/ebix", withCertificate: testCertificate)
            .Build();

        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        Assert.True(_nextSpy.NextWasCalled);
    }

    [Fact]
    public async Task When_calling_authentication_middleware_with_certificate_then_authenticated_actor_is_correct()
    {
        // Arrange
        var expectedActorNumber = "1234567891234";
        using var testCertificate = CreateTestCertificate();
        await CreateActorCertificatesInDatabase(withActorNumber: expectedActorNumber, withThumbprint: testCertificate.Thumbprint);

        var functionContext = _functionContextBuilder
            .TriggeredByHttp("application/ebix", withCertificate: testCertificate)
            .Build();

        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        Assert.Equal(expectedActorNumber, AuthenticatedActor.CurrentActorIdentity.ActorNumber.Value);
    }

    [Fact]
    public async Task When_calling_authentication_middleware_without_certificate_then_next_is_not_called()
    {
        // Arrange
        var functionContext = _functionContextBuilder
            .TriggeredByHttp("application/ebix", withCertificate: null)
            .Build();
        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        Assert.False(_nextSpy.NextWasCalled);
    }

    [Fact]
    public async Task When_calling_authentication_middleware_without_certificate_then_the_http_response_is_unauthorized()
    {
        // Arrange
        var functionContext = _functionContextBuilder
            .TriggeredByHttp("application/ebix", withCertificate: null)
            .Build();
        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        var response = functionContext.GetHttpResponse();
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task When_calling_authentication_middleware_with_incorrect_certificate_then_next_is_not_called()
    {
        // Arrange
        await CreateActorCertificatesInDatabase(withThumbprint: "incorrect-thumbprint");

        using var testCertificate = CreateTestCertificate();
        var functionContext = _functionContextBuilder
            .TriggeredByHttp("application/ebix", withCertificate: testCertificate)
            .Build();
        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        Assert.False(_nextSpy.NextWasCalled);
    }

    [Fact]
    public async Task When_calling_authentication_middleware_without_certificate_then_authenticated_actor_throws_exception()
    {
        // Arrange
        using var testCertificate = CreateTestCertificate();
        await CreateActorCertificatesInDatabase(withThumbprint: testCertificate.Thumbprint);

        var functionContext = _functionContextBuilder
            .TriggeredByHttp("application/ebix", withCertificate: null)
            .Build();

        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        Assert.Throws<InvalidOperationException>(() => AuthenticatedActor.CurrentActorIdentity.ActorNumber.Value);
    }

    [Fact]
    public async Task When_calling_authentication_middleware_with_incorrect_certificate_then_authenticated_actor_throws_exception()
    {
        // Arrange
        using var testCertificate = CreateTestCertificate();
        await CreateActorCertificatesInDatabase(withThumbprint: "incorrect-thumbprint");

        var functionContext = _functionContextBuilder
            .TriggeredByHttp("application/ebix", withCertificate: null)
            .Build();

        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        Assert.Throws<InvalidOperationException>(() => AuthenticatedActor.CurrentActorIdentity.ActorNumber.Value);
    }

    [Fact]
    public async Task When_calling_authentication_middleware_with_no_content_type_then_bearer_authentication_is_used()
    {
        var externalId = "external-id";

        // Arrange
        await CreateActorInDatabaseAsync(ActorNumber.Create("1234567891234"), externalId);
        var token = new JwtBuilder()
            .WithRole("energysupplier")
            .WithClaim(ClaimsMap.UserId, externalId)
            .CreateToken();

        var functionContext = _functionContextBuilder
            .TriggeredByHttp(withContentType: null, withToken: token)
            .Build();

        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        Assert.True(_nextSpy.NextWasCalled);
    }

    private static MarketActorAuthenticatorMiddleware CreateMarketActorAuthenticatorMiddleware()
    {
        var stubLogger = new Logger<MarketActorAuthenticatorMiddleware>(NullLoggerFactory.Instance);

        var sut = new MarketActorAuthenticatorMiddleware(stubLogger);
        return sut;
    }

    private static X509Certificate2 CreateTestCertificate()
    {
        using var ecdsa = ECDsa.Create();
        var req = new CertificateRequest("cn=self-created-test-certificate", ecdsa, HashAlgorithmName.SHA256);
        var certificate = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        return certificate;
    }

    private async Task CreateActorCertificatesInDatabase(string? withActorNumber = null, string? withThumbprint = null)
    {
        await CreateActorCertificateInDatabaseAsync(ActorNumber.Create("random-number-01"), ActorRole.EnergySupplier, "random-thumbprint-1");
        await CreateActorCertificateInDatabaseAsync(ActorNumber.Create("random-number-02"), ActorRole.EnergySupplier, "random-thumbprint-2");
        await CreateActorCertificateInDatabaseAsync(ActorNumber.Create(withActorNumber ?? "random-number-03"), ActorRole.EnergySupplier, withThumbprint ?? "random-thumbprint-3");
    }

    private async Task CreateActorCertificateInDatabaseAsync(ActorNumber actorNumber, ActorRole actorRole, string thumbprint)
    {
        var connectionFactory = GetService<IDatabaseConnectionFactory>();

        using var connection = await connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        await connection.ExecuteAsync("INSERT INTO [dbo].[ActorCertificate] VALUES (@id, @actorNumber, @actorRole, @thumbprint, @validFrom, @sequenceNumber)", new
        {
            id = Guid.NewGuid(),
            actorNumber = actorNumber.Value,
            actorRole = actorRole.Code,
            thumbprint = thumbprint,
            validFrom = Instant.FromUtc(2023, 1, 1, 0, 0),
            sequenceNumber = 1,
        });
    }

    private async Task CreateActorInDatabaseAsync(ActorNumber actorNumber, string externalId)
    {
        var connectionFactory = GetService<IDatabaseConnectionFactory>();

        using var connection = await connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        await connection.ExecuteAsync("INSERT INTO [dbo].[Actor] VALUES (@id, @actorNumber, @externalId)", new
        {
            id = Guid.NewGuid(),
            actorNumber = actorNumber.Value,
            externalId = externalId,
        });
    }
}
