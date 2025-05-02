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

using System.Globalization;
using System.Net;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.B2BApi.Functions.BundleMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.Shared.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using MeasurementUnit = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeasurementUnit;
using MeteringPointType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType;
using Quality = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Quality;
using Resolution = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Resolution;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_021;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs21CalculationMessagesTests : IAsyncLifetime
{
    private readonly B2BApiAppFixture _fixture;

    public EnqueueBrs21CalculationMessagesTests(
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

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null!);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task
        Given_EnqueueCalculatedMeasurementsHttpV1_When_MessageIsReceived_AndWhen_MessageIsBundled_Then_MessageIsEnqueued_AndThen_MessageCanBePeeked()
    {
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagName.PM25Messages, true),
            new(FeatureFlagName.PM25CIM, true),
        ]);

        // Arrange
        // => Given enqueue BRS-021 service bus message
        const string receiver1ActorNumber = "1111111111111";
        var receiver1ActorRole = ActorRole.EnergySupplier;
        const int receiver1Quantity = 11;

        const string receiver2ActorNumber = "2222222222222";
        var receiver2ActorRole = ActorRole.EnergySupplier;
        const int receiver2Quantity = 22;

        var startDateTime = Instant.FromUtc(2025, 01, 31, 23, 00, 00);

        var receiver1Start = startDateTime;
        var receiver1End = startDateTime.Plus(Duration.FromMinutes(15));

        var receiver2Start = receiver1End;
        var receiver2End = receiver2Start.Plus(Duration.FromMinutes(15));

        var enqueueMessagesData = new EnqueueCalculatedMeasurementsHttpV1(
            OrchestrationInstanceId: Guid.NewGuid(),
            TransactionId: Guid.NewGuid(),
            MeteringPointId: "1234567890123",
            MeteringPointType: MeteringPointType.Consumption,
            Resolution: Resolution.QuarterHourly,
            MeasureUnit: MeasurementUnit.KilowattHour,
            //ProductNumber: "test-product-number",
            Data:
            [
                new EnqueueCalculatedMeasurementsHttpV1.ReceiversWithMeasurements(
                    Receivers:
                    [
                        new EnqueueCalculatedMeasurementsHttpV1.Actor(
                            ActorNumber: ActorNumber.Create(receiver1ActorNumber).ToProcessManagerActorNumber(),
                            ActorRole: receiver1ActorRole.ToProcessManagerActorRole()),
                    ],
                    StartDateTime: receiver1Start.ToDateTimeOffset(),
                    EndDateTime: receiver1End.ToDateTimeOffset(),
                    RegistrationDateTime: startDateTime.ToDateTimeOffset(),
                    GridAreaCode: "804",
                    Measurements:
                    [
                        new EnqueueCalculatedMeasurementsHttpV1.Measurement(
                            Position: 1,
                            EnergyQuantity: receiver1Quantity,
                            QuantityQuality: Quality.AsProvided),
                    ]),
                new EnqueueCalculatedMeasurementsHttpV1.ReceiversWithMeasurements(
                    Receivers:
                    [
                        new EnqueueCalculatedMeasurementsHttpV1.Actor(
                            ActorNumber: ActorNumber.Create(receiver2ActorNumber).ToProcessManagerActorNumber(),
                            ActorRole: receiver2ActorRole.ToProcessManagerActorRole()),
                    ],
                    StartDateTime: receiver2Start.ToDateTimeOffset(),
                    EndDateTime: receiver2End.ToDateTimeOffset(),
                    RegistrationDateTime: startDateTime.ToDateTimeOffset(),
                    GridAreaCode: "804",
                    Measurements:
                    [
                        new EnqueueCalculatedMeasurementsHttpV1.Measurement(
                            Position: 1,
                            EnergyQuantity: receiver2Quantity,
                            QuantityQuality: Quality.AsProvided),
                    ]),
            ]);

        // Act
        // => When message is received
        var httpRequest = await _fixture.CreateEnqueueCalculatedMeasurementsHttpV1RequestAsync(
            enqueueMessagesData,
            "electricalHeatingOrSomething");

        await _fixture.AppHostManager.HttpClient.SendAsync(httpRequest);

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
            .Where(om => om.DocumentType == DocumentType.NotifyValidatedMeasureData)
            .ToListAsync();

        enqueuedOutgoingMessages.Should().HaveCount(2);

        // => Verify that the enqueued message can be peeked
        List<(Actor Actor, decimal EnergyQuantity, Instant Start, Instant End)> expectedReceivers =
        [
            (new Actor(ActorNumber.Create(receiver1ActorNumber), receiver1ActorRole),
                receiver1Quantity,
                receiver1Start,
                receiver1End),
            (new Actor(ActorNumber.Create(receiver2ActorNumber), receiver2ActorRole),
                receiver2Quantity,
                receiver2Start,
                receiver2End),
        ];

        foreach (var expectedReceiver in expectedReceivers)
        {
            var peekHttpRequest = await _fixture.CreatePeekHttpRequestAsync(
                actor: expectedReceiver.Actor,
                category: MessageCategory.MeasureData);

            var peekResponse = await _fixture.AppHostManager.HttpClient.SendAsync(peekHttpRequest);
            await peekResponse.EnsureSuccessStatusCodeWithLogAsync(_fixture.TestLogger);

            // Ensure status code is 200 OK, since EnsureSuccessStatusCode() also allows 204 No Content
            peekResponse.StatusCode.Should()
                .Be(
                    HttpStatusCode.OK,
                    $"because the peek request for receiver {expectedReceiver.Actor.ActorNumber} should return OK status code (with content)");

            var peekResponseContent = await peekResponse.Content.ReadAsStringAsync();
            peekResponseContent.Should()
                .NotBeNullOrEmpty()
                .And.Contain(
                    "NotifyValidatedMeasureData",
                    $"because the peeked messages for receiver {expectedReceiver.Actor.ActorNumber} should be a notify validated measure data")
                .And.Contain(
                    $"\"quantity\": {expectedReceiver.EnergyQuantity}",
                    $"because the peeked messages for receiver {expectedReceiver.Actor.ActorNumber} should have the expected measure data")
                .And.Contain(
                    $"\"value\": \"{expectedReceiver.Start.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture)}\"",
                    $"because the peeked messages for receiver {expectedReceiver.Actor.ActorNumber} should have the expected start")
                .And.Contain(
                    $"\"value\": \"{expectedReceiver.End.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture)}\"",
                    $"because the peeked messages for receiver {expectedReceiver.Actor.ActorNumber} should have the expected end");
        }
    }
}
