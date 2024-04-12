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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Retention;

public class RemoveInternalCommandsWhenADayHasPassedTests : TestBase
{
    private readonly ProcessContext _processContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly InternalCommandsRetention _sut;

    public RemoveInternalCommandsWhenADayHasPassedTests(
        IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processContext = GetService<ProcessContext>();
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
        _sut = new InternalCommandsRetention(GetService<IDatabaseConnectionFactory>(), GetService<ILogger<InternalCommandsRetention>>());
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
            var processedCommand = new QueuedInternalCommand(Guid.NewGuid(), string.Empty, string.Empty, _systemDateTimeProvider.Now());
            processedCommand.ProcessedDate = _systemDateTimeProvider.Now();
            _processContext.QueuedInternalCommands.Add(processedCommand);
        }

        for (int i = 0; i < amountOfNotProcessedInternalCommands; i++)
        {
            var notProcessedCommand = new QueuedInternalCommand(Guid.NewGuid(), string.Empty, string.Empty, _systemDateTimeProvider.Now());
            _processContext.QueuedInternalCommands.Add(notProcessedCommand);
        }

        await _processContext.SaveChangesAsync(CancellationToken.None);
    }
}
