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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages.Bundling;

public class WhenEnqueuingMeasureDataWithBundlingTests : OutgoingMessagesTestBase
{
    public WhenEnqueuingMeasureDataWithBundlingTests(
        OutgoingMessagesTestFixture outgoingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task
        Given_10000MessagesToEnqueue_When_EnqueueingMessages_Then_MessagesAreEnqueuedIn5Bundles()
    {
        var startTime = Instant.FromUtc(2024, 03, 21, 23, 00, 00);
        const int messageCount = 10000;
        var messages = Enumerable.Range(0, messageCount)
            .Select(
                i =>
                {
                    var resolutionDuration = Duration.FromMinutes(15);
                    var time = startTime.Plus(i * resolutionDuration); // Start every message 15 minutes later
                    return new AcceptedForwardMeteredDataMessageDto(
                        eventId: EventId.From(Guid.NewGuid()),
                        externalId: new ExternalId(Guid.NewGuid()),
                        receiver: new Actor(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier),
                        businessReason: BusinessReason.PeriodicMetering,
                        relatedToMessageId: MessageId.New(),
                        series: new ForwardMeteredDataMessageSeriesDto(
                            TransactionId: TransactionId.New(),
                            MarketEvaluationPointNumber: "1234567890123",
                            MarketEvaluationPointType: MeteringPointType.Consumption,
                            OriginalTransactionIdReferenceId: TransactionId.New(),
                            Product: "test-product",
                            QuantityMeasureUnit: MeasurementUnit.KilowattHour,
                            RegistrationDateTime: time,
                            Resolution: Resolution.QuarterHourly,
                            StartedDateTime: time,
                            EndedDateTime: time.Plus(resolutionDuration),
                            EnergyObservations:
                            [
                                new EnergyObservationDto(1, 1, Quality.Calculated),
                            ]));
                });

        // When enqueueing the messages concurrently
        var tasks = messages.Select(
            async message =>
            {
                await using var scope = ServiceProvider.CreateAsyncScope();
                var outgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();

                await outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);

                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await unitOfWork.CommitTransactionAsync(CancellationToken.None);
            });

        await Task.WhenAll(tasks);
    }
}
