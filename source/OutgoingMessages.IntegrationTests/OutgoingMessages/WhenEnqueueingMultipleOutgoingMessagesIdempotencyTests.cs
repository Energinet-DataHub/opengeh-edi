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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MissingMeasurementMessages;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using RejectReason = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint.RejectReason;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages;

/// <summary>
/// Tests the Outgoing Messages idempotency logic, which is the unique combination of the following 4 properties:
/// 1. Receiver actor number
/// 2. Receiver actor role
/// 3. External id
/// 4. Start period
/// </summary>
public class WhenEnqueueingMultipleOutgoingMessagesIdempotencyTests : OutgoingMessagesTestBase
{
    public WhenEnqueueingMultipleOutgoingMessagesIdempotencyTests(
        OutgoingMessagesTestFixture outgoingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task Given_TwoMessagesWithSameIdempotencyData_When_EnqueueingForwardMeteredDataSeparately_Then_OneMessageIsEnqueued()
    {
        // Given multiple messages with the same idempotency data
        var externalId = ExternalId.New();
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.GridAccessProvider);
        var start = Instant.FromUtc(2024, 12, 31, 23, 00);
        var end = Instant.FromUtc(2025, 01, 31, 23, 00);

        var relatedToMessageId1 = MessageId.New();
        var message1 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: receiver,
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId1);

        var relatedToMessageId2 = MessageId.New();
        var message2 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: receiver,
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId2);

        // When enqueueing the messages
        var outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var unitOfWork = ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        // We need to save changes between enqueues to not fail on idempotency unique constraint in database
        await outgoingMessagesClient.EnqueueAsync(message1, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await outgoingMessagesClient.EnqueueAsync(message2, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then only one message is enqueued
        using var queryScope = ServiceProvider.CreateScope();
        var outgoingMessagesContext = queryScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var outgoingMessages = await outgoingMessagesContext.OutgoingMessages.ToListAsync();

        // Assert that the collection contains exactly one element, with the expected RelatedToMessageId
        Assert.Multiple(
            () => Assert.Single(outgoingMessages),
            () => Assert.Equal(relatedToMessageId1, outgoingMessages.First().RelatedToMessageId),
            () => Assert.NotEqual(relatedToMessageId2, outgoingMessages.First().RelatedToMessageId));
    }

    [Fact]
    public async Task Given_TwoMessagesWithSameIdempotencyData_When_EnqueueingForwardMeteredDataAtTheSameTime_Then_UniqueDatabaseIndexThrowsException()
    {
        // Given multiple messages with the same idempotency data
        var externalId = ExternalId.New();
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.GridAccessProvider);
        var start = Instant.FromUtc(2024, 12, 31, 23, 00);
        var end = Instant.FromUtc(2025, 01, 31, 23, 00);

        var relatedToMessageId1 = MessageId.New();
        var message1 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: receiver,
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId1);

        var relatedToMessageId2 = MessageId.New();
        var message2 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: receiver,
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId2);

        // When enqueueing the messages
        var outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var dbContext = ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        await outgoingMessagesClient.EnqueueAsync(message1, CancellationToken.None);
        await outgoingMessagesClient.EnqueueAsync(message2, CancellationToken.None);

        var act = () => dbContext.SaveChangesAsync(CancellationToken.None);

        await act.Should()
            .ThrowExactlyAsync<DbUpdateException>()
            .WithInnerException(typeof(SqlException))
            .WithMessage(
                "Cannot insert duplicate key row in object 'dbo.OutgoingMessages' with unique index 'UQ_OutgoingMessages_ReceiverNumber_PeriodStartedAt_ReceiverRole_ExternalId'*");
    }

    [Fact]
    public async Task Given_TwoMessagesWithDifferentReceiverActorNumbers_When_EnqueueingForwardMeteredData_Then_BothMessagesAreEnqueued()
    {
        // Given multiple messages with the same idempotency data except for the receiver actor number
        var externalId = ExternalId.New();
        var start = Instant.FromUtc(2024, 12, 31, 23, 00);
        var end = Instant.FromUtc(2025, 01, 31, 23, 00);

        var relatedToMessageId1 = MessageId.New();
        var message1 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: new Actor(ActorNumber.Create("1234567890123"), ActorRole.GridAccessProvider),
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId1);

        var relatedToMessageId2 = MessageId.New();
        var message2 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: new Actor(ActorNumber.Create("1111111111111"), ActorRole.GridAccessProvider),
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId2);

        // When enqueueing the messages
        var outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        await outgoingMessagesClient.EnqueueAsync(message1, CancellationToken.None);
        await outgoingMessagesClient.EnqueueAsync(message2, CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        // Then both messages are enqueued
        using var queryScope = ServiceProvider.CreateScope();
        var outgoingMessagesContext = queryScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var outgoingMessages = await outgoingMessagesContext.OutgoingMessages.ToListAsync();

        // Asserts that the collection contains exactly 2 elements, with the expected RelatedToMessageId's
        Assert.Collection(
            outgoingMessages.OrderBy(om => om.CreatedAt),
            [
                om => Assert.Equal(relatedToMessageId1, om.RelatedToMessageId),
                om => Assert.Equal(relatedToMessageId2, om.RelatedToMessageId),
            ]);
    }

    [Fact]
    public async Task Given_TwoMessagesWithDifferentReceiverActorRoles_When_EnqueueingForwardMeteredData_Then_BothMessagesAreEnqueued()
    {
        // Given multiple messages with the same idempotency data except for the receiver actor number
        var externalId = ExternalId.New();
        var start = Instant.FromUtc(2024, 12, 31, 23, 00);
        var end = Instant.FromUtc(2025, 01, 31, 23, 00);

        var relatedToMessageId1 = MessageId.New();
        var message1 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: new Actor(ActorNumber.Create("1234567890123"), ActorRole.GridAccessProvider),
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId1);

        var relatedToMessageId2 = MessageId.New();
        var message2 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier),
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId2);

        // When enqueueing the messages
        var outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        await outgoingMessagesClient.EnqueueAsync(message1, CancellationToken.None);
        await outgoingMessagesClient.EnqueueAsync(message2, CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        // Then both messages are enqueued
        using var queryScope = ServiceProvider.CreateScope();
        var outgoingMessagesContext = queryScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var outgoingMessages = await outgoingMessagesContext.OutgoingMessages.ToListAsync();

        // Asserts that the collection contains exactly 2 elements, with the expected RelatedToMessageId's
        Assert.Collection(
            outgoingMessages.OrderBy(om => om.CreatedAt),
            [
                om => Assert.Equal(relatedToMessageId1, om.RelatedToMessageId),
                om => Assert.Equal(relatedToMessageId2, om.RelatedToMessageId),
            ]);
    }

    [Fact]
    public async Task Given_TwoMessagesWithDifferentExternalIds_When_EnqueueingForwardMeteredData_Then_BothMessagesAreEnqueued()
    {
        // Given multiple messages with the same idempotency data except for the receiver actor number
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.GridAccessProvider);
        var start = Instant.FromUtc(2024, 12, 31, 23, 00);
        var end = Instant.FromUtc(2025, 01, 31, 23, 00);

        var relatedToMessageId1 = MessageId.New();
        var message1 = CreateAcceptedForwardMeteredDataMessage(
            externalId: ExternalId.New(),
            receiver: receiver,
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId1);

        var relatedToMessageId2 = MessageId.New();
        var message2 = CreateAcceptedForwardMeteredDataMessage(
            externalId: ExternalId.New(),
            receiver: receiver,
            start: start,
            end: end,
            relatedToMessageId: relatedToMessageId2);

        // When enqueueing the messages
        var outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        await outgoingMessagesClient.EnqueueAsync(message1, CancellationToken.None);
        await outgoingMessagesClient.EnqueueAsync(message2, CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        // Then both messages are enqueued
        using var queryScope = ServiceProvider.CreateScope();
        var outgoingMessagesContext = queryScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var outgoingMessages = await outgoingMessagesContext.OutgoingMessages.ToListAsync();

        // Asserts that the collection contains exactly 2 elements, with the expected RelatedToMessageId's
        Assert.Collection(
            outgoingMessages.OrderBy(om => om.CreatedAt),
            [
                om => Assert.Equal(relatedToMessageId1, om.RelatedToMessageId),
                om => Assert.Equal(relatedToMessageId2, om.RelatedToMessageId),
            ]);
    }

    [Fact]
    public async Task Given_TwoMessagesWithDifferentStartPeriods_When_EnqueueingForwardMeteredData_Then_BothMessagesAreEnqueued()
    {
        // Given multiple messages with the same idempotency data except for the receiver actor number
        var externalId = ExternalId.New();
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.GridAccessProvider);
        var end = Instant.FromUtc(2025, 01, 31, 23, 00);

        var start1 = Instant.FromUtc(2024, 12, 31, 23, 00);
        var relatedToMessageId1 = MessageId.New();
        var message1 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: receiver,
            start: start1,
            end: end,
            relatedToMessageId: relatedToMessageId1);

        var start2 = Instant.FromUtc(2025, 01, 15, 23, 00);
        var relatedToMessageId2 = MessageId.New();
        var message2 = CreateAcceptedForwardMeteredDataMessage(
            externalId: externalId,
            receiver: receiver,
            start: start2,
            end: end,
            relatedToMessageId: relatedToMessageId2);

        // When enqueueing the messages
        var outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        await outgoingMessagesClient.EnqueueAsync(message1, CancellationToken.None);
        await outgoingMessagesClient.EnqueueAsync(message2, CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        // Then both messages are enqueued
        using var queryScope = ServiceProvider.CreateScope();
        var outgoingMessagesContext = queryScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var outgoingMessages = await outgoingMessagesContext.OutgoingMessages.ToListAsync();

        // Asserts that the collection contains exactly 2 elements, with the expected RelatedToMessageId's
        Assert.Collection(
            outgoingMessages.OrderBy(om => om.CreatedAt),
            [
                om => Assert.Equal(relatedToMessageId1, om.RelatedToMessageId),
                om => Assert.Equal(relatedToMessageId2, om.RelatedToMessageId),
            ]);
    }

    [Fact]
    public async Task Given_TwoMessagesWithSameIdempotencyData_When_EnqueueRejectedForwardMeteredDataMessage_Then_OneMessageIsEnqueued()
    {
        // Arrange
        var serviceBusMessageId = Guid.NewGuid();
        var message = new RejectedSendMeasurementsMessageDto(
            eventId: EventId.From(serviceBusMessageId),
            externalId: new ExternalId(serviceBusMessageId),
            businessReason: BusinessReason.PeriodicFlexMetering,
            receiverNumber: ActorNumber.Create("1234567890123"),
            receiverRole: ActorRole.MeteredDataResponsible,
            documentReceiverRole: ActorRole.MeteredDataResponsible,
            relatedToMessageId: MessageId.New(),
            meteringPointId: MeteringPointId.From("1234567890123"),
            series: new RejectedForwardMeteredDataSeries(
                OriginalTransactionIdReference: TransactionId.New(),
                TransactionId: TransactionId.New(),
                RejectReasons: [
                    new RejectReason(
                        ErrorCode: "E0I",
                        ErrorMessage: "An error has occurred")]));

        // Act
        var outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        // We need to save changes between enqueues to not fail on idempotency unique constraint in database
        await outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        await outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        // Assert
        using var queryScope = ServiceProvider.CreateScope();
        var outgoingMessagesContext = queryScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var outgoingMessages = await outgoingMessagesContext.OutgoingMessages.ToListAsync();

        var outgoingMessage = Assert.Single(outgoingMessages);
        Assert.Equal(serviceBusMessageId.ToString(), outgoingMessage.ExternalId.Value);
    }

    [Fact]
    public async Task Given_TwoMessagesWithSameIdempotencyData_When_EnqueueingMissingMeasurementsLog_Then_OneMessageIsEnqueued()
    {
        var orchestrationInstanceId = Guid.NewGuid();
        var meteringPointId = MeteringPointId.From("1234567890123");
        var expectedExternalId = ExternalId.HashValuesWithMaxLength(
            orchestrationInstanceId.ToString("N"),
            meteringPointId.Value);

        var message = CreateMissingMeasurementsLogMessage(
            orchestrationInstanceId: orchestrationInstanceId,
            meteringPointId: meteringPointId,
            date: Instant.FromUtc(2024, 12, 31, 23, 00),
            receiverActorNumber: ActorNumber.Create("1234567890123"),
            receiverActorRole: ActorRole.GridAccessProvider);

        // Act
        var outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        // We need to save changes between enqueues to not fail on idempotency unique constraint in database
        await outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        await outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        // Assert
        using var queryScope = ServiceProvider.CreateScope();
        var outgoingMessagesContext = queryScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var outgoingMessages = await outgoingMessagesContext.OutgoingMessages.ToListAsync();

        var outgoingMessage = Assert.Single(outgoingMessages);
        Assert.Equal(expectedExternalId, outgoingMessage.ExternalId);
    }

    [Theory]
    [InlineData(MissingMeasurementsIdempotencyKey.OrchestrationInstanceId)]
    [InlineData(MissingMeasurementsIdempotencyKey.MeteringPointId)]
    [InlineData(MissingMeasurementsIdempotencyKey.Date)]
    [InlineData(MissingMeasurementsIdempotencyKey.ReceiverActorNumber)]
    [InlineData(MissingMeasurementsIdempotencyKey.ReceiverActorRole)]
    public async Task Given_TwoMessagesWithDifferentIdempotencyData_When_EnqueueingMissingMeasurementsLog_Then_BothMessagesAreEnqueued(
        MissingMeasurementsIdempotencyKey differentIdempotencyKey)
    {
        var orchestrationInstanceId1 = Guid.NewGuid();
        var orchestrationInstanceId2 = differentIdempotencyKey == MissingMeasurementsIdempotencyKey.OrchestrationInstanceId
            ? Guid.NewGuid()
            : orchestrationInstanceId1;

        var meteringPointId1 = MeteringPointId.From("1111111111111");
        var meteringPointId2 = differentIdempotencyKey == MissingMeasurementsIdempotencyKey.MeteringPointId
            ? MeteringPointId.From("2222222222222")
            : meteringPointId1;

        var date1 = Instant.FromUtc(2024, 12, 31, 23, 00);
        var date2 = differentIdempotencyKey == MissingMeasurementsIdempotencyKey.Date
            ? Instant.FromUtc(2025, 01, 31, 23, 00)
            : date1;

        const string receiverActorNumber1 = "1234567890123";
        var receiverActorNumber2 = differentIdempotencyKey == MissingMeasurementsIdempotencyKey.ReceiverActorNumber
            ? "1234567890123456"
            : receiverActorNumber1;

        var receiverActorRole1 = ActorRole.GridAccessProvider;
        var receiverActorRole2 = differentIdempotencyKey == MissingMeasurementsIdempotencyKey.ReceiverActorRole
            ? ActorRole.EnergySupplier
            : receiverActorRole1;

        var expectedExternalId1 = ExternalId.HashValuesWithMaxLength(
            orchestrationInstanceId1.ToString("N"),
            meteringPointId1.Value);
        var expectedExternalId2 = ExternalId.HashValuesWithMaxLength(
            orchestrationInstanceId2.ToString("N"),
            meteringPointId2.Value);

        // Given multiple messages with the same idempotency data except for the receiver actor number
        var message1 = CreateMissingMeasurementsLogMessage(
            orchestrationInstanceId1,
            meteringPointId1,
            date1,
            ActorNumber.Create(receiverActorNumber1),
            receiverActorRole1);

        var message2 = CreateMissingMeasurementsLogMessage(
            orchestrationInstanceId2,
            meteringPointId2,
            date2,
            ActorNumber.Create(receiverActorNumber2),
            receiverActorRole2);

        // Act
        var outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        await outgoingMessagesClient.EnqueueAsync(message1, CancellationToken.None);
        await outgoingMessagesClient.EnqueueAsync(message2, CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        // Then both messages are enqueued
        using var queryScope = ServiceProvider.CreateScope();
        var outgoingMessagesContext = queryScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var outgoingMessages = await outgoingMessagesContext.OutgoingMessages.ToListAsync();

        // Asserts that the collection contains exactly 2 elements, with the expected RelatedToMessageId's
        Assert.Collection(
            outgoingMessages.OrderBy(om => om.CreatedAt),
            [
                om => Assert.Equal(expectedExternalId1, om.ExternalId),
                om => Assert.Equal(expectedExternalId2, om.ExternalId),
            ]);
    }

    private AcceptedSendMeasurementsMessageDto CreateAcceptedForwardMeteredDataMessage(
        ExternalId externalId,
        Actor receiver,
        Instant start,
        Instant end,
        MessageId relatedToMessageId)
    {
        var resolution = Resolution.QuarterHourly;

        return new AcceptedSendMeasurementsMessageDto(
            eventId: EventId.From(Guid.NewGuid()),
            externalId: externalId,
            receiver: receiver,
            businessReason: BusinessReason.PeriodicMetering,
            relatedToMessageId: relatedToMessageId,
            gridAreaCode: "804",
            series: new MeasurementsDto(
                TransactionId: TransactionId.New(),
                MeteringPointId: "1234567890123",
                MeteringPointType: MeteringPointType.Consumption,
                OriginalTransactionIdReference: null,
                Product: "test-product",
                MeasurementUnit: MeasurementUnit.KilowattHour,
                RegistrationDateTime: start,
                Resolution: Resolution.QuarterHourly,
                Period: new Period(start, end),
                Measurements: GenerateEnergyObservations(start, end, resolution)));
    }

    private MissingMeasurementMessageDto CreateMissingMeasurementsLogMessage(
        Guid orchestrationInstanceId,
        MeteringPointId meteringPointId,
        Instant date,
        ActorNumber receiverActorNumber,
        ActorRole receiverActorRole)
    {
        return new MissingMeasurementMessageDto(
            eventId: EventId.From(Guid.NewGuid()),
            orchestrationInstanceId: orchestrationInstanceId,
            receiver: new Actor(
                receiverActorNumber,
                receiverActorRole),
            businessReason: BusinessReason.ReminderOfMissingMeasurementLog,
            gridAreaCode: "804",
            missingMeasurement: new MissingMeasurement(
                TransactionId: TransactionId.New(),
                MeteringPointId: meteringPointId,
                Date: date));
    }

    private IReadOnlyCollection<MeasurementDto> GenerateEnergyObservations(
        Instant start,
        Instant end,
        Resolution resolution)
    {
        var resolutionInMinutes = resolution switch
        {
            var r when r == Resolution.QuarterHourly => 15,
            var r when r == Resolution.Hourly => 60,
            var r when r == Resolution.Daily => 1440,
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unsupported resolution"),
        };

        var minutesInPeriod = (int)(end - start).TotalMinutes;
        var totalNumberOfObservations = minutesInPeriod / resolutionInMinutes;

        return Enumerable.Range(0, totalNumberOfObservations)
            .Select(i => new MeasurementDto(
                    Position: i + 1,
                    Quantity: 7,
                    Quality: Quality.Measured))
            .ToList();
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Test")]
    public enum MissingMeasurementsIdempotencyKey
    {
        OrchestrationInstanceId,
        MeteringPointId,
        Date,
        ReceiverActorNumber,
        ReceiverActorRole,
    }
}
