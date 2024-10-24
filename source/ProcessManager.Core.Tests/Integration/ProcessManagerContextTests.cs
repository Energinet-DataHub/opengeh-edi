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

using Energinet.DataHub.ProcessManagement.Core.Domain;
using Energinet.DataHub.ProcessManager.Core.Tests.Fixtures;
using FluentAssertions;
using Xunit.Abstractions;

namespace Energinet.DataHub.ProcessManager.Core.Tests.Integration;

[Collection(nameof(ProcessManagerCollectionFixture))]
public class ProcessManagerContextTests : IAsyncLifetime
{
    private readonly ProcessManagerDatabaseFixture _databaseFixture;
    private readonly ITestOutputHelper _output;

    public ProcessManagerContextTests(ProcessManagerDatabaseFixture databaseFixture, ITestOutputHelper output)
    {
        _databaseFixture = databaseFixture;
        _output = output;
    }

    public Task InitializeAsync()
    {
        throw new NotImplementedException();
    }

    public Task DisposeAsync()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "WIP")]
    public async Task Given_OrchestrationDescription_WhenAddedToContext_HasCorrectValues()
    {
        // Arrange
        var newOrchestrationDescription = new OrchestrationDescription(
            "TestOrchestration",
            4,
            true,
            "TestOrchestrationFunction");

        newOrchestrationDescription
            .ParameterDefinition
            .SetFromType<TestOrchestrationParameter>();

        // Act
        using (var writeDbContext = _databaseFixture.DatabaseManager.CreateDbContext())
        {
            writeDbContext.OrchestrationDescriptions.Add(newOrchestrationDescription);
            await writeDbContext.SaveChangesAsync();
        }

        // Assert
        using (var readDbContext = _databaseFixture.DatabaseManager.CreateDbContext())
        {
            var orchestrationDescription = await readDbContext.OrchestrationDescriptions.FindAsync(newOrchestrationDescription.Id);

            orchestrationDescription.Should()
                .NotBeNull()
                .And
                .BeEquivalentTo(newOrchestrationDescription);
        }
    }

    private class TestOrchestrationParameter
    {
        public string? TestString { get; set; }

        public int? TestInt { get; set; }
    }
}
