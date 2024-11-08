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

using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationDescription;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Database;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Orchestration;
using Energinet.DataHub.ProcessManager.Core.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Moq;
using NodaTime;

namespace Energinet.DataHub.ProcessManager.Core.Tests.Integration.Infrastructure.Orchestration;

[Collection(nameof(ProcessManagerCoreCollection))]
public class OrchestrationInstanceRepositoryTests : IAsyncLifetime
{
    private readonly ProcessManagerCoreFixture _fixture;
    private readonly ProcessManagerContext _dbContext;
    private readonly OrchestrationInstanceRepository _sut;
    private readonly UnitOfWork _unitOfWork;

    public OrchestrationInstanceRepositoryTests(ProcessManagerCoreFixture fixture)
    {
        _fixture = fixture;
        _dbContext = _fixture.DatabaseManager.CreateDbContext();
        _sut = new OrchestrationInstanceRepository(_dbContext);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
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
        var act = _unitOfWork.CommitAsync;

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
            runAt: SystemClock.Instance.GetCurrentInstant().PlusMinutes(1));
        await _sut.AddAsync(scheduledToRun);

        var scheduledIntoTheFarFuture = CreateOrchestrationInstance(
            existingOrchestrationDescription,
            runAt: SystemClock.Instance.GetCurrentInstant().PlusDays(5));
        await _sut.AddAsync(scheduledIntoTheFarFuture);

        await _unitOfWork.CommitAsync();

        // Act
        var actual = await _sut.FindAsync(
            SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(5)));

        // Assert
        actual.Should()
            .ContainEquivalentOf(scheduledToRun)
            .And
            .HaveCount(1);
    }

    [Fact]
    public async Task GivenOrchestrationInstancesInDatabase_WhenSearchByName_ThenExpectedOrchestrationInstancesAreRetrieved()
    {
        // Arrange
        var uniqueName1 = Guid.NewGuid().ToString();
        var existingOrchestrationDescription01 = CreateOrchestrationDescription(uniqueName1, version: 1);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescription01);

        var uniqueName2 = Guid.NewGuid().ToString();
        var existingOrchestrationDescription02 = CreateOrchestrationDescription(uniqueName2, version: 1);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescription02);

        var basedOn01 = CreateOrchestrationInstance(existingOrchestrationDescription01);
        await _sut.AddAsync(basedOn01);

        var basedOn02 = CreateOrchestrationInstance(existingOrchestrationDescription02);
        await _sut.AddAsync(basedOn02);

        await _unitOfWork.CommitAsync();

        // Act
        var actual = await _sut.SearchAsync(existingOrchestrationDescription01.Name);

        // Assert
        actual.Should()
            .BeEquivalentTo(new[] { basedOn01 });
    }

    [Fact]
    public async Task GivenOrchestrationInstancesInDatabase_WhenSearchByNameAndVersion_ThenExpectedOrchestrationInstancesAreRetrieved()
    {
        // Arrange
        var uniqueName = Guid.NewGuid().ToString();
        var existingOrchestrationDescriptionV1 = CreateOrchestrationDescription(uniqueName, version: 1);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescriptionV1);

        var existingOrchestrationDescriptionV2 = CreateOrchestrationDescription(uniqueName, version: 2);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescriptionV2);

        var basedOnV1 = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        await _sut.AddAsync(basedOnV1);

        var basedOnV2 = CreateOrchestrationInstance(existingOrchestrationDescriptionV2);
        await _sut.AddAsync(basedOnV2);

        await _unitOfWork.CommitAsync();

        // Act
        var actual = await _sut.SearchAsync(existingOrchestrationDescriptionV1.Name, existingOrchestrationDescriptionV1.Version);

        // Assert
        actual.Should()
            .BeEquivalentTo(new[] { basedOnV1 });
    }

    [Fact]
    public async Task GivenOrchestrationInstancesInDatabase_WhenSearchByNameAndLifecycleState_ThenExpectedOrchestrationInstancesAreRetrieved()
    {
        // Arrange
        var uniqueName = Guid.NewGuid().ToString();
        var existingOrchestrationDescriptionV1 = CreateOrchestrationDescription(uniqueName, version: 1);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescriptionV1);

        var existingOrchestrationDescriptionV2 = CreateOrchestrationDescription(uniqueName, version: 2);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescriptionV2);

        var isPendingV1 = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        await _sut.AddAsync(isPendingV1);

        var isRunningV1 = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        isRunningV1.Lifecycle.TransitionToQueued(SystemClock.Instance);
        isRunningV1.Lifecycle.TransitionToRunning(SystemClock.Instance);
        await _sut.AddAsync(isRunningV1);

        var isPendingV2 = CreateOrchestrationInstance(existingOrchestrationDescriptionV2);
        await _sut.AddAsync(isPendingV2);

        var isRunningV2 = CreateOrchestrationInstance(existingOrchestrationDescriptionV2);
        isRunningV2.Lifecycle.TransitionToQueued(SystemClock.Instance);
        isRunningV2.Lifecycle.TransitionToRunning(SystemClock.Instance);
        await _sut.AddAsync(isRunningV2);

        await _unitOfWork.CommitAsync();

        // Act
        var actual = await _sut.SearchAsync(existingOrchestrationDescriptionV1.Name, lifecycleState: OrchestrationInstanceLifecycleStates.Running);

        // Assert
        actual.Should()
            .BeEquivalentTo(new[] { isRunningV1, isRunningV2 });
    }

    [Fact]
    public async Task GivenOrchestrationInstancesInDatabase_WhenSearchByNameAndTerminationState_ThenExpectedOrchestrationInstancesAreRetrieved()
    {
        // Arrange
        var uniqueName = Guid.NewGuid().ToString();
        var existingOrchestrationDescriptionV1 = CreateOrchestrationDescription(uniqueName, version: 1);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescriptionV1);

        var existingOrchestrationDescriptionV2 = CreateOrchestrationDescription(uniqueName, version: 2);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescriptionV2);

        var isPendingV1 = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        await _sut.AddAsync(isPendingV1);

        var isTerminatedAsSucceededV1 = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        isTerminatedAsSucceededV1.Lifecycle.TransitionToQueued(SystemClock.Instance);
        isTerminatedAsSucceededV1.Lifecycle.TransitionToRunning(SystemClock.Instance);
        isTerminatedAsSucceededV1.Lifecycle.TransitionToTerminated(SystemClock.Instance, OrchestrationInstanceTerminationStates.Succeeded);
        await _sut.AddAsync(isTerminatedAsSucceededV1);

        var isPendingV2 = CreateOrchestrationInstance(existingOrchestrationDescriptionV2);
        await _sut.AddAsync(isPendingV2);

        var isTerminatedAsFailedV2 = CreateOrchestrationInstance(existingOrchestrationDescriptionV2);
        isTerminatedAsFailedV2.Lifecycle.TransitionToQueued(SystemClock.Instance);
        isTerminatedAsFailedV2.Lifecycle.TransitionToRunning(SystemClock.Instance);
        isTerminatedAsFailedV2.Lifecycle.TransitionToTerminated(SystemClock.Instance, OrchestrationInstanceTerminationStates.Failed);
        await _sut.AddAsync(isTerminatedAsFailedV2);

        await _unitOfWork.CommitAsync();

        // Act
        var actual = await _sut.SearchAsync(
            existingOrchestrationDescriptionV1.Name,
            lifecycleState: OrchestrationInstanceLifecycleStates.Terminated,
            terminationState: OrchestrationInstanceTerminationStates.Succeeded);

        // Assert
        actual.Should()
            .BeEquivalentTo(new[] { isTerminatedAsSucceededV1 });
    }

    [Fact]
    public async Task GivenOrchestrationInstancesInDatabase_WhenSearchByNameAndStartedAt_ThenExpectedOrchestrationInstancesAreRetrieved()
    {
        // Arrange
        var startedAt01 = SystemClock.Instance.GetCurrentInstant().PlusDays(1);
        var startedAtClockMock01 = new Mock<IClock>();
        startedAtClockMock01.Setup(m => m.GetCurrentInstant())
            .Returns(startedAt01);

        var uniqueName = Guid.NewGuid().ToString();
        var existingOrchestrationDescriptionV1 = CreateOrchestrationDescription(uniqueName, version: 1);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescriptionV1);

        var isPending = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        await _sut.AddAsync(isPending);

        var isRunning01 = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        isRunning01.Lifecycle.TransitionToQueued(SystemClock.Instance);
        isRunning01.Lifecycle.TransitionToRunning(startedAtClockMock01.Object);
        await _sut.AddAsync(isRunning01);

        var isRunning02 = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        isRunning02.Lifecycle.TransitionToQueued(SystemClock.Instance);
        isRunning02.Lifecycle.TransitionToRunning(SystemClock.Instance);
        await _sut.AddAsync(isRunning02);

        await _unitOfWork.CommitAsync();

        // Act
        var actual = await _sut.SearchAsync(
            existingOrchestrationDescriptionV1.Name,
            startedAtOrLater: startedAt01);

        // Assert
        actual.Should()
            .BeEquivalentTo(new[] { isRunning01 });
    }

    [Fact]
    public async Task GivenOrchestrationInstancesInDatabase_WhenSearchByNameAndTerminatedAt_ThenExpectedOrchestrationInstancesAreRetrieved()
    {
        // Arrange
        var terminatedAt01 = SystemClock.Instance.GetCurrentInstant().PlusDays(-1);
        var terminatedAtClockMock01 = new Mock<IClock>();
        terminatedAtClockMock01.Setup(m => m.GetCurrentInstant())
            .Returns(terminatedAt01);

        var uniqueName = Guid.NewGuid().ToString();
        var existingOrchestrationDescriptionV1 = CreateOrchestrationDescription(uniqueName, version: 1);
        await SeedDatabaseWithOrchestrationDescriptionAsync(existingOrchestrationDescriptionV1);

        var isPending = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        await _sut.AddAsync(isPending);

        var isRunning = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        isRunning.Lifecycle.TransitionToQueued(SystemClock.Instance);
        isRunning.Lifecycle.TransitionToRunning(SystemClock.Instance);
        await _sut.AddAsync(isRunning);

        var isTerminated01 = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        isTerminated01.Lifecycle.TransitionToQueued(SystemClock.Instance);
        isTerminated01.Lifecycle.TransitionToRunning(SystemClock.Instance);
        isTerminated01.Lifecycle.TransitionToTerminated(terminatedAtClockMock01.Object, OrchestrationInstanceTerminationStates.Succeeded);
        await _sut.AddAsync(isTerminated01);

        var isTerminated02 = CreateOrchestrationInstance(existingOrchestrationDescriptionV1);
        isTerminated02.Lifecycle.TransitionToQueued(SystemClock.Instance);
        isTerminated02.Lifecycle.TransitionToRunning(SystemClock.Instance);
        isTerminated02.Lifecycle.TransitionToTerminated(SystemClock.Instance, OrchestrationInstanceTerminationStates.Succeeded);
        await _sut.AddAsync(isTerminated02);

        await _unitOfWork.CommitAsync();

        // Act
        var actual = await _sut.SearchAsync(
            existingOrchestrationDescriptionV1.Name,
            terminatedAtOrEarlier: terminatedAt01);

        // Assert
        actual.Should()
            .BeEquivalentTo(new[] { isTerminated01 });
    }

    private static OrchestrationDescription CreateOrchestrationDescription(string? name = default, int? version = default)
    {
        var orchestrationDescription = new OrchestrationDescription(
            name: name ?? "TestOrchestration",
            version: version ?? 4,
            canBeScheduled: true,
            functionName: "TestOrchestrationFunction");

        orchestrationDescription.ParameterDefinition.SetFromType<TestOrchestrationParameter>();

        orchestrationDescription.AppendStepDescription("Test step 1");
        orchestrationDescription.AppendStepDescription("Test step 2");
        orchestrationDescription.AppendStepDescription("Test step 3");

        return orchestrationDescription;
    }

    private static OrchestrationInstance CreateOrchestrationInstance(
        OrchestrationDescription orchestrationDescription,
        Instant? runAt = default)
    {
        var orchestrationInstance = OrchestrationInstance.CreateFromDescription(
            description: orchestrationDescription,
            clock: SystemClock.Instance,
            runAt: runAt);

        orchestrationInstance.ParameterValue.SetFromInstance(new TestOrchestrationParameter
        {
            TestString = "Test string",
            TestInt = 42,
        });

        return orchestrationInstance;
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
