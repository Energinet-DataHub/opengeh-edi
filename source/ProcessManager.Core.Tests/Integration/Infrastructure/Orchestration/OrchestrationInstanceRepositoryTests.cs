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
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Orchestration;
using Energinet.DataHub.ProcessManager.Core.Tests.Fixtures;
using FluentAssertions;
using NodaTime;

namespace Energinet.DataHub.ProcessManager.Core.Tests.Integration.Infrastructure.Orchestration;

[Collection(nameof(ProcessManagerCoreCollection))]
public class OrchestrationInstanceRepositoryTests
{
    private readonly ProcessManagerCoreFixture _fixture;
    private readonly OrchestrationInstanceRepository _sut;

    public OrchestrationInstanceRepositoryTests(ProcessManagerCoreFixture fixture)
    {
        _fixture = fixture;
        _sut = new OrchestrationInstanceRepository(_fixture.DatabaseManager.CreateDbContext());
    }

    [Fact]
    public async Task GivenIdNotInDatabase_WhenGetById_ThenThrowsException()
    {
        // Arrange
        var id = new OrchestrationInstanceId(Guid.NewGuid());

        // Act
        var act = () => _sut.GetAsync(id);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Sequence contains no elements.");
    }

    [Fact]
    public async Task GivenIdInDatabase_WhenGetById_ThenExpectedOrchestrationInstanceIsRetrieved()
    {
        // Arrange
        var existingOrchestrationDescription = CreateOrchestrationDescription();
        var existingOrchestrationInstance = CreateOrchestrationInstance(existingOrchestrationDescription);

        await using (var writeDbContext = _fixture.DatabaseManager.CreateDbContext())
        {
            writeDbContext.OrchestrationDescriptions.Add(existingOrchestrationDescription);
            writeDbContext.OrchestrationInstances.Add(existingOrchestrationInstance);
            await writeDbContext.SaveChangesAsync();
        }

        // Act
        var actual = await _sut.GetAsync(existingOrchestrationInstance.Id);

        // Assert
        actual.Should()
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

    private static OrchestrationInstance CreateOrchestrationInstance(OrchestrationDescription existingOrchestrationDescription)
    {
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

        return existingOrchestrationInstance;
    }

    private class TestOrchestrationParameter
    {
        public string? TestString { get; set; }

        public int? TestInt { get; set; }
    }
}
