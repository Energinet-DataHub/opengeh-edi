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
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Database;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Orchestration;
using Energinet.DataHub.ProcessManager.Core.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;

namespace Energinet.DataHub.ProcessManager.Core.Tests.Integration.Infrastructure.Orchestration;

[Collection(nameof(ProcessManagerCoreCollection))]
public class OrchestrationInstanceRepositoryTests
{
    private readonly ProcessManagerCoreFixture _fixture;
    private readonly OrchestrationInstanceRepository _sut;
    private readonly UnitOfWork _unitOfWork;

    public OrchestrationInstanceRepositoryTests(ProcessManagerCoreFixture fixture)
    {
        _fixture = fixture;
        var dbContext = _fixture.DatabaseManager.CreateDbContext();
        _sut = new OrchestrationInstanceRepository(dbContext);
        _unitOfWork = new UnitOfWork(dbContext);
    }

    [Fact]
    public async Task GivenOrchestrationInstanceIdNotInDatabase_WhenGetById_ThenThrowsException()
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
    public async Task GivenOrchestrationInstanceIdInDatabase_WhenGetById_ThenExpectedOrchestrationInstanceIsRetrieved()
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

    [Fact]
    public async Task GivenOrchestrationDescriptionNotInDatabase_WhenAddOrchestrationInstance_ThenThrowsException()
    {
        // Arrange
        var newOrchestrationDescription = CreateOrchestrationDescription();
        var newOrchestrationInstance = CreateOrchestrationInstance(newOrchestrationDescription);

        // Act
        await _sut.AddAsync(newOrchestrationInstance);
        var act = () => _unitOfWork.CommitAsync();

        // Assert
        await act.Should()
            .ThrowAsync<DbUpdateException>()
            .WithInnerException<DbUpdateException, SqlException>()
            .WithMessage("*FOREIGN KEY constraint \"FK_OrchestrationInstance_OrchestrationDescription\"*");
    }

    [Fact]
    public async Task GivenOrchestrationDescriptionInDatabase_WhenAddOrchestrationInstance_ThenOrchestrationInstanceIsAdded()
    {
        // Arrange
        var existingOrchestrationDescription = CreateOrchestrationDescription();
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescription);

        var newOrchestrationInstance = CreateOrchestrationInstance(existingOrchestrationDescription);

        // Act
        await _sut.AddAsync(newOrchestrationInstance);
        await _unitOfWork.CommitAsync();

        // Assert
        var actual = await _sut.GetAsync(newOrchestrationInstance.Id);
        actual.Should()
            .BeEquivalentTo(newOrchestrationInstance);
    }

    [Fact]
    public async Task GivenScheduledOrchestrationInstancesInDatabase_WhenGetScheduledByInstant_ThenExpectedOrchestrationInstancesAreRetrieved()
    {
        // Arrange
        var existingOrchestrationDescription = CreateOrchestrationDescription();
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescription);

        var notScheduled = CreateOrchestrationInstance(existingOrchestrationDescription);
        await _sut.AddAsync(notScheduled);

        var scheduledToRun = CreateOrchestrationInstance(
            existingOrchestrationDescription,
            scheduledToRunAt: SystemClock.Instance.GetCurrentInstant().PlusMinutes(1));
        await _sut.AddAsync(scheduledToRun);

        var scheduledIntoTheFarFuture = CreateOrchestrationInstance(
            existingOrchestrationDescription,
            scheduledToRunAt: SystemClock.Instance.GetCurrentInstant().PlusDays(5));
        await _sut.AddAsync(scheduledIntoTheFarFuture);

        await _unitOfWork.CommitAsync();

        // Act
        var actual = await _sut.GetScheduledByInstantAsync(
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(5)));

        // Assert
        actual.Should()
            .ContainEquivalentOf(scheduledToRun)
            .And
            .HaveCount(1);
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

    private static OrchestrationInstance CreateOrchestrationInstance(OrchestrationDescription existingOrchestrationDescription, Instant? scheduledToRunAt = default)
    {
        var existingOrchestrationInstance = new OrchestrationInstance(
            existingOrchestrationDescription.Id,
            SystemClock.Instance,
            scheduledToRunAt);

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

    private async Task SeedDatabaseWithOrchestrationDescriptionAsync(OrchestrationDescription existingOrchestrationDescription)
    {
        await using (var writeDbContext = _fixture.DatabaseManager.CreateDbContext())
        {
            writeDbContext.OrchestrationDescriptions.Add(existingOrchestrationDescription);
            await writeDbContext.SaveChangesAsync();
        }
    }

    private class TestOrchestrationParameter
    {
        public string? TestString { get; set; }

        public int? TestInt { get; set; }
    }
}
