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
        SetupServiceCollection();
    }

    private ServiceCollection ServiceCollection { get; } = [];

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
        var outboxMessagePublisher = new Mock<IOutboxPublisher>();
        var serviceProvider = ServiceCollection
            .AddTransient<IOutboxPublisher>(_ => outboxMessagePublisher.Object)
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

        var actualMessage = readContext.Outbox.SingleOrDefault(om => om.Id == outboxMessage.Id);

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
        var outboxMessagePublisher = new Mock<IOutboxPublisher>();
        var clock = new Mock<IClock>();
        var serviceProvider = ServiceCollection
            .AddTransient<IOutboxPublisher>(_ => outboxMessagePublisher.Object)
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

        var actualMessage = readContext.Outbox.SingleOrDefault(om => om.Id == outboxMessage.Id);

        actualMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();
        actualMessage!.FailedAt.Should().Be(now);
        actualMessage.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        actualMessage.ErrorCount.Should().Be(1);
        actualMessage.PublishedAt.Should().BeNull();
        actualMessage.ProcessingAt.Should().BeNull();
    }

    [Fact]
    public async Task Given_FailedOutboxMessage_When_ProcessingOutboxMessageBeforeRetryTimeout_Then_MessageIsNotRetried()
    {
        // Arrange
        var outboxMessageType = "message-type-1";
        var outboxMessagePublisher = new Mock<IOutboxPublisher>();
        outboxMessagePublisher
            .Setup(omp => omp.CanProcess(outboxMessageType))
            .Returns(true);

        var failedAt = Instant.FromUtc(2024, 09, 02, 13, 37);
        var clock = new Mock<IClock>();
        clock.Setup(c => c.GetCurrentInstant())
            .Returns(failedAt);

        var serviceProvider = ServiceCollection
            .AddTransient<IOutboxPublisher>(_ => outboxMessagePublisher.Object)
            .AddTransient<IClock>(_ => clock.Object)
            .BuildServiceProvider();

        var processor = serviceProvider.GetRequiredService<IOutboxProcessor>();

        var outboxMessage = new OutboxMessage(outboxMessageType, "mock-payload");
        outboxMessage.SetAsFailed(clock.Object, "an-error-message");
        using (var writeScope = serviceProvider.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var writeContext = writeScope.ServiceProvider.GetRequiredService<OutboxContext>();

            repository.Add(outboxMessage);
            await writeContext.SaveChangesAsync();
        }

        // Act
        // => Clock is set to the same time as the message failed, so the message is not ready to be retried yet
        await processor.ProcessOutboxAsync();

        // Assert
        using var readScope = serviceProvider.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<OutboxContext>();

        var actualMessage = readContext.Outbox.SingleOrDefault(om => om.Id == outboxMessage.Id);

        actualMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();

        // => Error count should still be 1, and the message should not be published or processing
        actualMessage!.ErrorCount.Should().Be(1);
        actualMessage.PublishedAt.Should().BeNull();
        actualMessage.ProcessingAt.Should().BeNull();

        // => Publish should not have been called
        outboxMessagePublisher
            .Verify(
                omp => omp.PublishAsync(It.IsAny<string>()),
                Times.Never);
    }

    [Fact]
    public async Task Given_FailedOutboxMessageInThePast_When_ProcessingOutboxMessageAfterRetryTimeout_Then_MessageIsRetriedAndPublished()
    {
        // Arrange
        var outboxMessageType = "message-type-1";
        var outboxMessagePublisher = new Mock<IOutboxPublisher>();
        outboxMessagePublisher
            .Setup(omp => omp.CanProcess(outboxMessageType))
            .Returns(true);

        var inThePast = Instant.FromUtc(2024, 09, 02, 12, 0);
        var now = Instant.FromUtc(2024, 09, 02, 13, 37);

        var clock = new Mock<IClock>();

        var serviceProvider = ServiceCollection
            .AddTransient<IOutboxPublisher>(_ => outboxMessagePublisher.Object)
            .AddTransient<IClock>(_ => clock.Object)
            .BuildServiceProvider();

        var processor = serviceProvider.GetRequiredService<IOutboxProcessor>();

        var outboxMessage = new OutboxMessage(outboxMessageType, "mock-payload");

        // => Set clock to a time in the past, so SetAsFailed() sets FailedAt to sometime in the past
        clock.Setup(c => c.GetCurrentInstant())
            .Returns(inThePast);
        outboxMessage.SetAsFailed(clock.Object, "an-error-message");

        using (var writeScope = serviceProvider.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var writeContext = writeScope.ServiceProvider.GetRequiredService<OutboxContext>();

            repository.Add(outboxMessage);
            await writeContext.SaveChangesAsync();
        }

        // => Change clock to now, so the message is ready to be retried
        clock.Setup(c => c.GetCurrentInstant())
            .Returns(now);

        // Act
        await processor.ProcessOutboxAsync();

        // Assert
        using var readScope = serviceProvider.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<OutboxContext>();

        var actualMessage = readContext.Outbox.SingleOrDefault(om => om.Id == outboxMessage.Id);

        actualMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();

        // => The message failed in the past, but should be published now
        actualMessage!.FailedAt.Should().Be(inThePast);
        actualMessage.PublishedAt.Should().Be(now);

        // => Publish should have been called once
        outboxMessagePublisher
            .Verify(
                omp => omp.PublishAsync(It.IsAny<string>()),
                Times.Once);
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
                })
            .Build();

        ServiceCollection
            .AddSingleton<IConfiguration>(config)
            .AddB2BAuthentication(JwtTokenParserTests.DisableAllTokenValidations)
            .AddSystemTimer()
            .AddOutboxModule(config)
            .AddOutboxProcessor()
            .AddScoped<ExecutionContext>((x) =>
            {
                var executionContext = new ExecutionContext();
                executionContext.SetExecutionType(ExecutionType.Test);
                return executionContext;
            });
    }
}
