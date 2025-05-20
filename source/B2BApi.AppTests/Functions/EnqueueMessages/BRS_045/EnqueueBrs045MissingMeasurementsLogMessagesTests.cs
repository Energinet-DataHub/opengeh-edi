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

using System.Net;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.B2BApi.Functions.BundleMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.FeatureManagement;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
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
            new(FeatureFlagNames.PeekMeasurementMessages, true),
            new(FeatureFlagNames.PM25CIM, true),
        ]);

        // Arrange
        // => Given enqueue BRS-045 http request
        var gridAccessProviderActorNumber = ActorNumber.Create("1111111111111");
        var dateWithMeasurement = new EnqueueMissingMeasurementsLogHttpV1.DateWithMeteringPointId(
            IdempotencyKey: Guid.NewGuid(),
            GridAccessProvider: gridAccessProviderActorNumber.ToProcessManagerActorNumber(),
            GridArea: "123",
            Date: Instant.FromUtc(2025, 05, 01, 22, 00).ToDateTimeOffset(),
            MeteringPointId: "1234567890123");

        var enqueueMessagesData = new EnqueueMissingMeasurementsLogHttpV1(
            OrchestrationInstanceId: Guid.NewGuid(),
            Data:
            [
                dateWithMeasurement,
                dateWithMeasurement with
                {
                    MeteringPointId = dateWithMeasurement.MeteringPointId + "2",
                    IdempotencyKey = Guid.NewGuid(),
                },
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
             .Where(om => om.DocumentType == DocumentType.ReminderOfMissingMeasureData)
             .ToListAsync();

        enqueuedOutgoingMessages.Should().HaveCount(enqueueMessagesData.Data.Count);

        var receiver = new Actor(
            gridAccessProviderActorNumber,
            ActorRole.GridAccessProvider);

        var peekHttpRequest = await _fixture.CreatePeekHttpRequestAsync(
            actor: receiver,
            category: MessageCategory.MeasureData);

        var peekResponse = await _fixture.AppHostManager.HttpClient.SendAsync(peekHttpRequest);
        await peekResponse.EnsureSuccessStatusCodeWithLogAsync(_fixture.TestLogger);

        var content = await peekResponse.Content.ReadAsStringAsync(cancellationToken: CancellationToken.None);
        content.Should().Contain("ReminderOfMissingMeasureData_MarketDocument", "because the peek response should contain MissingMeasurementsLog documents");
    }
}
