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
using NodaTime;

namespace Energinet.DataHub.ProcessManager.Core.Tests.Integration.Infrastructure.Database;

[Collection(nameof(ProcessManagerCoreCollection))]
public class ProcessManagerContextTests
{
    private readonly ProcessManagerCoreFixture _fixture;

    public ProcessManagerContextTests(ProcessManagerCoreFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Given_OrchestrationDescriptionAddedToDbContext_WhenRetrievingFromDatabase_HasCorrectValues()
    {
        // Arrange
        var existingOrchestrationDescription = CreateOrchestrationDescription();

        await using (var writeDbContext = _fixture.DatabaseManager.CreateDbContext())
        {
            writeDbContext.OrchestrationDescriptions.Add(existingOrchestrationDescription);
            await writeDbContext.SaveChangesAsync();
        }

        // Act
        await using var readDbContext = _fixture.DatabaseManager.CreateDbContext();
        var orchestrationDescription = await readDbContext.OrchestrationDescriptions.FindAsync(existingOrchestrationDescription.Id);

        // Assert
        orchestrationDescription.Should()
            .NotBeNull()
            .And
            .BeEquivalentTo(existingOrchestrationDescription);
    }

    [Fact]
    public async Task Given_OrchestrationInstanceWithStepsAddedToDbContext_WhenRetrievingFromDatabase_HasCorrectValues()
    {
        // Arrange
        var existingOrchestrationDescription = CreateOrchestrationDescription();
        var existingOrchestrationInstance = new OrchestrationInstance(
            existingOrchestrationDescription.Id,
            SystemClock.Instance);

        var step1 = new OrchestrationStep(
            existingOrchestrationInstance.Id,
            SystemClock.Instance,
            "Test step 1",
            0);

        var step2 = new OrchestrationStep(
            existingOrchestrationInstance.Id,
            SystemClock.Instance,
            "Test step 2",
            1,
            step1.Id);

        var step3 = new OrchestrationStep(
            existingOrchestrationInstance.Id,
            SystemClock.Instance,
            "Test step 3",
            2,
            step2.Id);

        existingOrchestrationInstance.Steps.Add(step1);
        existingOrchestrationInstance.Steps.Add(step2);
        existingOrchestrationInstance.Steps.Add(step3);

        existingOrchestrationInstance.ParameterValue.SetFromInstance(new TestOrchestrationParameter
        {
            TestString = "Test string",
            TestInt = 42,
        });

        await using (var writeDbContext = _fixture.DatabaseManager.CreateDbContext())
        {
            writeDbContext.OrchestrationDescriptions.Add(existingOrchestrationDescription);
            writeDbContext.OrchestrationInstances.Add(existingOrchestrationInstance);
            await writeDbContext.SaveChangesAsync();
        }

        // Act
        await using var readDbContext = _fixture.DatabaseManager.CreateDbContext();
        var orchestrationInstance = await readDbContext.OrchestrationInstances.FindAsync(existingOrchestrationInstance.Id);

        // Assert
        orchestrationInstance.Should()
            .NotBeNull()
            .And
            .BeEquivalentTo(existingOrchestrationInstance);
    }

    private static OrchestrationDescription CreateOrchestrationDescription()
    {
        var existingOrchestrationDescription = new OrchestrationDescription(
            "TestOrchestration",
            4,
            true,
            "TestOrchestrationFunction");

        existingOrchestrationDescription
            .ParameterDefinition
            .SetFromType<TestOrchestrationParameter>();
        return existingOrchestrationDescription;
    }

    private class TestOrchestrationParameter
    {
        public string? TestString { get; set; }

        public int? TestInt { get; set; }
    }
}
