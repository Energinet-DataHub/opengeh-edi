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
using Application.Configuration;
using Application.Configuration.DataAccess;
using Infrastructure.Configuration.DataAccess;
using Infrastructure.Configuration.InternalCommands;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests.Infrastructure.Retention;

public class RemoveInternalCommandsWhenADayHasPassedTests : TestBase
{
    private readonly B2BContext _b2BContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly InternalCommandsRetention _sut;

    public RemoveInternalCommandsWhenADayHasPassedTests(
        DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
        _sut = new InternalCommandsRetention(GetService<IDatabaseConnectionFactory>());
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
        _b2BContext.Dispose();
        base.Dispose(disposing);
    }

    private void AssertProcessedInternalCommandIsRemoved(int amountOfNotProcessedInternalCommands)
    {
        var proccessedInternalCommands = _b2BContext.QueuedInternalCommands
            .Where(command => command.ProcessedDate != null);
        var notProccessedInternalCommands = _b2BContext.QueuedInternalCommands
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
            _b2BContext.QueuedInternalCommands.Add(processedCommand);
        }

        for (int i = 0; i < amountOfNotProcessedInternalCommands; i++)
        {
            var notProcessedCommand = new QueuedInternalCommand(Guid.NewGuid(), string.Empty, string.Empty, _systemDateTimeProvider.Now());
            _b2BContext.QueuedInternalCommands.Add(notProcessedCommand);
        }

        await _b2BContext.SaveChangesAsync(CancellationToken.None);
    }
}
