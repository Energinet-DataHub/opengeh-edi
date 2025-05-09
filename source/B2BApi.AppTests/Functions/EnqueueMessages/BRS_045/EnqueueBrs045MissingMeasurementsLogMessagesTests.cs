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

using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.B2BApi.Functions.BundleMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_045.MissingMeasurementsLogCalculation.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_045;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs045MissingMeasurementsLogMessagesTests : IAsyncLifetime
{
    private readonly B2BApiAppFixture _fixture;

    public EnqueueBrs045MissingMeasurementsLogMessagesTests(
        B2BApiAppFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(testOutputHelper);
    }

    public async Task InitializeAsync()
    {
        _fixture.AppHostManager.ClearHostLog();

        // Dequeue existing messages
        await using var context = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();

        var bundles = await context.Bundles.ToListAsync();
        foreach (var bundle in bundles)
        {
            bundle.TryDequeue();
        }

        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null!);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task
        Given_EnqueueMissingMeasurementsLogHttpV1_When_MessageIsReceived_AndWhen_MessageIsBundled_Then_MessageIsEnqueued_AndThen_MessageCanBePeeked()
    {
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagName.PeekMeasurementMessages, true),
            new(FeatureFlagName.PM25CIM, true),
        ]);

        // Arrange
        // => Given enqueue BRS-045 http request
        var gridAccessProviderActorNumber = ActorNumber.Create("1111111111111");
        var gridAccessProviderRole = ActorRole.GridAccessProvider;

        var enqueueMessagesData = new EnqueueMissingMeasurementsLogHttpV1(
            OrchestrationInstanceId: Guid.NewGuid(),
            MeteringPointId: "1234567890123",
            GridAccessProvider: gridAccessProviderActorNumber.ToProcessManagerActorNumber(),
            GridArea: "123",
            MissingDates:
            [
                Instant.FromUtc(2025, 05, 01, 22, 00).ToDateTimeOffset(),
                Instant.FromUtc(2025, 05, 08, 22, 00).ToDateTimeOffset(),
                Instant.FromUtc(2025, 05, 09, 22, 00).ToDateTimeOffset()
            ]);

        // Act
        // => When message is received
        var httpRequest = _fixture.CreateEnqueueMessagesHttpRequest(enqueueMessagesData);

        var response = await _fixture.AppHostManager.HttpClient.SendAsync(httpRequest);
        await response.EnsureSuccessStatusCodeWithLogAsync(_fixture.TestLogger);

        // => And when message is bundled
        await _fixture.AppHostManager.TriggerFunctionAsync(nameof(OutgoingMessagesBundler));

        // Verify the bundling function was executed
        var bundleFunctionResult =
            await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
                functionName: nameof(OutgoingMessagesBundler));

        bundleFunctionResult.Succeeded.Should()
            .BeTrue(
                "the OutgoingMessagesBundler function should have been completed with success. Host log:\n{0}",
                bundleFunctionResult.HostLog);

        using var assertionScope = new AssertionScope();

        // => Verify that outgoing messages were enqueued
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var enqueuedOutgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.DocumentType == DocumentType.NotifyValidatedMeasureData) // TODO: Update to missing measurements log
            .ToListAsync();

        // TODO: Verify that the enqueued messages can be peeked
        // - enqueuedOutgoingMessages.Should().HaveCount(3);
        // - Peek all messages (expect 3)
        // - Verify that the messages has correct document type
        // - Verify that the messages has correct metering point id
        // - Verify that the messages has correct dates
    }
}
