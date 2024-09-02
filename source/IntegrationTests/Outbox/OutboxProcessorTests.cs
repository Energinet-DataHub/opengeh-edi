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

using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Energinet.DataHub.EDI.Outbox.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Outbox.Domain;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using Energinet.DataHub.EDI.Outbox.Interfaces;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using Xunit;
using ExecutionContext = Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext;

namespace Energinet.DataHub.EDI.IntegrationTests.Outbox;

public class OutboxProcessorTests : IClassFixture<OutboxTestFixture>, IAsyncLifetime
{
    public OutboxProcessorTests(OutboxTestFixture fixture)
    {
        Fixture = fixture;

        var dbConnectionString = Fixture.DatabaseManager.ConnectionString;
        if (!dbConnectionString.Contains("Trust")) // Trust Server Certificate might be required for some
            dbConnectionString = $"{dbConnectionString};Trust Server Certificate=True;";
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", Fixture.DatabaseManager.ConnectionString);

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "DB_CONNECTION_STRING", dbConnectionString },
                })
            .Build();

        ServiceCollection = [];

        ServiceCollection
            .AddB2BAuthentication(JwtTokenParserTests.DisableAllTokenValidations)
            .AddSystemTimer()
            .AddOutboxModule(config)
            .AddOutboxProcessor();

        ServiceCollection.AddScoped<ExecutionContext>((x) =>
        {
            var executionContext = new ExecutionContext();
            executionContext.SetExecutionType(ExecutionType.Test);
            return executionContext;
        });
    }

    public ServiceCollection ServiceCollection { get; set; }

    private OutboxTestFixture Fixture { get; }

    public async Task InitializeAsync()
    {
        await Fixture.TruncateDatabaseTablesAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task When_PublishingOutboxMessage_Then_MessageCorrectlyProcessed()
    {
        // Arrange
        var clock = new Mock<IClock>();
        var outboxMessagePublisher = new Mock<IOutboxMessagePublisher>();
        var serviceProvider = ServiceCollection
            .AddTransient<IOutboxMessagePublisher>(_ => outboxMessagePublisher.Object)
            .AddTransient<IClock>(_ => clock.Object)
            .BuildServiceProvider();
        var now = Instant.FromUtc(2024, 09, 02, 13, 37);

        var processor = serviceProvider.GetRequiredService<IOutboxProcessor>();

        var outboxMessage = new OutboxMessage("mock-type", "mock-payload");
        using (var writeScope = serviceProvider.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var writeContext = writeScope.ServiceProvider.GetRequiredService<OutboxContext>();

            repository.Add(outboxMessage);
            await writeContext.SaveChangesAsync();
        }

        outboxMessagePublisher
            .Setup(omp => omp.CanProcess("mock-type"))
            .Returns(true);

        clock.Setup(c => c.GetCurrentInstant())
            .Returns(now);

        // Act
        await processor.ProcessOutboxAsync();

        // Assert
        using var readScope = serviceProvider.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<OutboxContext>();

        var actualMessage = readContext.OutboxMessages.SingleOrDefault(om => om.Id == outboxMessage.Id);

        actualMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();
        actualMessage!.PublishedAt.Should().Be(now);
        actualMessage.FailedAt.Should().BeNull();
        actualMessage.ErrorMessage.Should().BeNull();
        actualMessage.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_OutboxMessagePublisherThrowsException_When_ProcessingOutboxMessage_Then_MessageIsSetAsFailed()
    {
        // Arrange
        var outboxMessagePublisher = new Mock<IOutboxMessagePublisher>();
        var clock = new Mock<IClock>();
        var serviceProvider = ServiceCollection
            .AddTransient<IOutboxMessagePublisher>(_ => outboxMessagePublisher.Object)
            .AddTransient<IClock>(_ => clock.Object)
            .BuildServiceProvider();
        var now = Instant.FromUtc(2024, 09, 02, 13, 37);

        var processor = serviceProvider.GetRequiredService<IOutboxProcessor>();

        var outboxMessage = new OutboxMessage("mock-type", "mock-payload");
        using (var writeScope = serviceProvider.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var writeContext = writeScope.ServiceProvider.GetRequiredService<OutboxContext>();

            repository.Add(outboxMessage);
            await writeContext.SaveChangesAsync();
        }

        outboxMessagePublisher
            .Setup(omp => omp.CanProcess("mock-type"))
            .Returns(true);

        clock.Setup(c => c.GetCurrentInstant())
            .Returns(now);

        outboxMessagePublisher
            .Setup(omp => omp.PublishAsync(It.IsAny<string>()))
            .Throws<Exception>();

        // Act
        await processor.ProcessOutboxAsync();

        // Assert
        using var readScope = serviceProvider.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<OutboxContext>();

        var actualMessage = readContext.OutboxMessages.SingleOrDefault(om => om.Id == outboxMessage.Id);

        actualMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();
        actualMessage!.FailedAt.Should().Be(now);
        actualMessage.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        actualMessage.ErrorCount.Should().Be(1);
        actualMessage.PublishedAt.Should().BeNull();
        actualMessage.ProcessingAt.Should().BeNull();
    }
}
