﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.Core.Outbox.Infrastructure.DbContext;
using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Retention;

public class RemoveInternalCommandsWhenADayHasPassedTests : TestBase
{
    private readonly ProcessContext _processContext;
    private readonly IClock _clock;
    private readonly InternalCommandsRetention _sut;

    public RemoveInternalCommandsWhenADayHasPassedTests(
        IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processContext = GetService<ProcessContext>();
        _clock = GetService<IClock>();
        _sut = new InternalCommandsRetention(
            GetService<IDatabaseConnectionFactory>(),
            GetService<ILogger<InternalCommandsRetention>>(),
            GetService<IAuditLogger>(),
            GetService<IClock>());

        // Retention jobs does not have an authenticated actor, so we need to set it to null.
        AuthenticatedActor.SetAuthenticatedActor(null);
    }

    [Fact]
    public async Task Clean_up_internal_commands_succeed()
    {
        // arrange
        var amountOfProcessedInternalCommands = 2500;
        var amountOfNotProcessedInternalCommands = 25;
        await GenerateInternalCommands(amountOfProcessedInternalCommands, amountOfNotProcessedInternalCommands);

        // Act
        await _sut.CleanupAsync(CancellationToken.None);

        // Assert
        AssertProcessedInternalCommandIsRemoved(amountOfNotProcessedInternalCommands);
    }

    [Fact]
    public async Task Clean_up_internal_commands_execution_is_being_audit_logged()
    {
        // arrange
        var serializer = GetService<ISerializer>();
        var amountOfProcessedInternalCommands = 2500;
        var amountOfNotProcessedInternalCommands = 25;
        await GenerateInternalCommands(amountOfProcessedInternalCommands, amountOfNotProcessedInternalCommands);

        // Act
        await _sut.CleanupAsync(CancellationToken.None);

        // Assert
        using var secondScope = ServiceProvider.CreateScope();
        var outboxContext = secondScope.ServiceProvider.GetRequiredService<IOutboxContext>();
        var outboxMessages = outboxContext.Outbox;
        var outboxMessage = outboxMessages.Should().NotBeEmpty().And.Subject.First();
        var payload = serializer.Deserialize<AuditLogOutboxMessageV1Payload>(outboxMessage.Payload);
        payload.Origin.Should().Be(nameof(ADayHasPassed));
        payload.AffectedEntityType.Should().Be(AuditLogEntityType.InternalCommand.Identifier);
    }

    protected override void Dispose(bool disposing)
    {
        _processContext.Dispose();
        base.Dispose(disposing);
    }

    private void AssertProcessedInternalCommandIsRemoved(int amountOfNotProcessedInternalCommands)
    {
        var proccessedInternalCommands = _processContext.QueuedInternalCommands
            .Where(command => command.ProcessedDate != null);
        var notProccessedInternalCommands = _processContext.QueuedInternalCommands
            .Where(command => command.ProcessedDate == null);

        Assert.Equal(amountOfNotProcessedInternalCommands, notProccessedInternalCommands.Count());
        Assert.Empty(proccessedInternalCommands);
    }

    private async Task GenerateInternalCommands(int amountOfProcessedInternalCommands, int amountOfNotProcessedInternalCommands)
    {
        for (int i = 0; i < amountOfProcessedInternalCommands; i++)
        {
            var processedCommand = new QueuedInternalCommand(Guid.NewGuid(), string.Empty, string.Empty, _clock.GetCurrentInstant());
            processedCommand.ProcessedDate = _clock.GetCurrentInstant();
            _processContext.QueuedInternalCommands.Add(processedCommand);
        }

        for (int i = 0; i < amountOfNotProcessedInternalCommands; i++)
        {
            var notProcessedCommand = new QueuedInternalCommand(Guid.NewGuid(), string.Empty, string.Empty, _clock.GetCurrentInstant());
            _processContext.QueuedInternalCommands.Add(notProcessedCommand);
        }

        await _processContext.SaveChangesAsync(CancellationToken.None);
    }
}
