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

using Azure;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Dequeue;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Retention;

public class RemoveOldDequeuedBundlesWhenADayHasPassedTests : TestBase
{
    private readonly WholesaleAmountPerChargeDtoBuilder _wholesaleAmountPerChargeDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;

    public RemoveOldDequeuedBundlesWhenADayHasPassedTests(
        IntegrationTestFixture integrationTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _wholesaleAmountPerChargeDtoBuilder = new WholesaleAmountPerChargeDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
    }

    [Fact]
    public async Task Clean_up_dequeued_bundles_when_they_are_more_than_a_month_old()
    {
        // Arrange
        var receiverId = ActorNumber.Create("1234567891912");
        var chargeOwnerId = ActorNumber.Create("1234567891911");
        var bundleRepository = GetService<IBundleRepository>();
        var actorMessageQueueContext = GetService<ActorMessageQueueContext>();
        var clockStub = new ClockStub();
        var actorMessageQueueRepository = GetService<IActorMessageQueueRepository>();
        var outgoingMessageRepository = GetService<IOutgoingMessageRepository>();
        var fileStorageClient = GetService<IFileStorageClient>();

        // When we set the current date to 31 days in the future, any bundles dequeued now should then be removed.
        clockStub.SetCurrentInstant(clockStub.GetCurrentInstant().PlusDays(31));

        var sut = new DequeuedBundlesRetention(
            clockStub,
            GetService<IMarketDocumentRepository>(),
            outgoingMessageRepository,
            actorMessageQueueContext,
            bundleRepository,
            GetService<ILogger<DequeuedBundlesRetention>>());

        var message = _wholesaleAmountPerChargeDtoBuilder
            .WithReceiverNumber(receiverId)
            .WithChargeOwnerNumber(chargeOwnerId)
            .Build();

        // We enqueue a message where the receiver is both a energy supplier and a grid operator. and then dequeue it only for the energy supplier.
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations, receiverId, ActorRole.EnergySupplier);
        await _outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(peekResult!.MessageId.Value, ActorRole.EnergySupplier, receiverId), CancellationToken.None);

        var outgoingMessageForReceivingActor = await outgoingMessageRepository.GetAsync(Receiver.Create(receiverId, ActorRole.EnergySupplier), message.ExternalId);

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        ClearDbContextCaches();
        var actorMessageQueueForEs = await actorMessageQueueRepository.ActorMessageQueueForAsync(receiverId, ActorRole.EnergySupplier);

        // The bundle should be removed from the queue for the energy supplier, but not for the grid operator.
        var dequeuedBundles = await bundleRepository.GetDequeuedBundlesOlderThanAsync(clockStub.GetCurrentInstant(), 100);
        dequeuedBundles.Should().NotContain(x => x.ActorMessageQueueId == actorMessageQueueForEs!.Id);

        // We are still able to peek the message for the grid operator.
        var peekResultForGo = await PeekMessageAsync(MessageCategory.Aggregations, chargeOwnerId, ActorRole.GridOperator);
        peekResultForGo.Should().NotBeNull();

        // blob should be cleaned up
        var downloadBlob = () => fileStorageClient.DownloadAsync(outgoingMessageForReceivingActor!.FileStorageReference);
        await downloadBlob.Should().ThrowAsync<RequestFailedException>();
    }

    [Fact]
    public async Task Clean_up_dequeued_bundle_when_its_blob_does_not_exist()
    {
        // Arrange
        var receiverId = ActorNumber.Create("1234567891912");
        var chargeOwnerId = ActorNumber.Create("1234567891911");
        var bundleRepository = GetService<IBundleRepository>();
        var actorMessageQueueContext = GetService<ActorMessageQueueContext>();
        var clockStub = new ClockStub();
        var actorMessageQueueRepository = GetService<IActorMessageQueueRepository>();
        var outgoingMessageRepository = GetService<IOutgoingMessageRepository>();
        var fileStorageClient = GetService<IFileStorageClient>();

        // When we set the current date to 31 days in the future, any bundles dequeued now should then be removed.
        clockStub.SetCurrentInstant(clockStub.GetCurrentInstant().PlusDays(31));

        var sut = new DequeuedBundlesRetention(
            clockStub,
            GetService<IMarketDocumentRepository>(),
            outgoingMessageRepository,
            actorMessageQueueContext,
            bundleRepository,
            GetService<ILogger<DequeuedBundlesRetention>>());

        var message = _wholesaleAmountPerChargeDtoBuilder
            .WithReceiverNumber(receiverId)
            .WithChargeOwnerNumber(chargeOwnerId)
            .Build();

        // We enqueue a message where the receiver is both a energy supplier and a grid operator. and then dequeue it only for the energy supplier.
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations, receiverId, ActorRole.EnergySupplier);
        await _outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(peekResult!.MessageId.Value, ActorRole.EnergySupplier, receiverId), CancellationToken.None);

        var outgoingMessageForReceivingActor = await outgoingMessageRepository.GetAsync(Receiver.Create(receiverId, ActorRole.EnergySupplier), message.ExternalId);

        // Delete the blob
        await fileStorageClient.DeleteIfExistsAsync(new List<FileStorageReference> { outgoingMessageForReceivingActor!.FileStorageReference }, FileStorageCategory.OutgoingMessage());

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        ClearDbContextCaches();
        var actorMessageQueueForEs = await actorMessageQueueRepository.ActorMessageQueueForAsync(receiverId, ActorRole.EnergySupplier);

        // The bundle should be removed from the queue for the energy supplier, but not for the grid operator.
        var dequeuedBundles = await bundleRepository.GetDequeuedBundlesOlderThanAsync(clockStub.GetCurrentInstant(), 100);
        dequeuedBundles.Should().NotContain(x => x.ActorMessageQueueId == actorMessageQueueForEs!.Id);

        // We are still able to peek the message for the grid operator.
        var peekResultForGo = await PeekMessageAsync(MessageCategory.Aggregations, chargeOwnerId, ActorRole.GridOperator);
        peekResultForGo.Should().NotBeNull();
    }

    [Fact]
    public async Task Clean_up_501_dequeued_bundles_when_they_are_more_than_a_month_old()
    {
        // Arrange
        var receiverId = ActorNumber.Create("1234567891912");
        var chargeOwnerId = ActorNumber.Create("1234567891911");
        var bundleRepository = GetService<IBundleRepository>();
        var actorMessageQueueContext = GetService<ActorMessageQueueContext>();
        var clockStub = new ClockStub();
        var actorMessageQueueRepository = GetService<IActorMessageQueueRepository>();
        var outgoingMessageRepository = GetService<IOutgoingMessageRepository>();
        var fileStorageClient = GetService<IFileStorageClient>();

        // When we set the current date to 31 days in the future, any bundles dequeued now should then be removed.
        clockStub.SetCurrentInstant(clockStub.GetCurrentInstant().PlusDays(31));

        var sut = new DequeuedBundlesRetention(
            clockStub,
            GetService<IMarketDocumentRepository>(),
            outgoingMessageRepository,
            actorMessageQueueContext,
            bundleRepository,
            GetService<ILogger<DequeuedBundlesRetention>>());

        var outgoingMessages = new List<OutgoingMessage>();
        var numberOfMessageToCreate = 501;
        while (numberOfMessageToCreate > 0)
        {
            var message = _wholesaleAmountPerChargeDtoBuilder
                .WithReceiverNumber(receiverId)
                .WithChargeOwnerNumber(chargeOwnerId)
                .WithCalculationResultId(Guid.NewGuid())
                .Build();

            // We enqueue a message where the receiver is both a energy supplier and a grid operator. and then dequeue it only for the energy supplier.
            await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
            var peekResult = await PeekMessageAsync(MessageCategory.Aggregations, receiverId, ActorRole.EnergySupplier);
            await _outgoingMessagesClient.DequeueAndCommitAsync(
                new DequeueRequestDto(peekResult!.MessageId.Value, ActorRole.EnergySupplier, receiverId),
                CancellationToken.None);

            numberOfMessageToCreate--;
            var outgoingMessageForReceivingActor = await outgoingMessageRepository.GetAsync(Receiver.Create(receiverId, ActorRole.EnergySupplier), message.ExternalId);
            outgoingMessages.Add(outgoingMessageForReceivingActor!);
        }

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        ClearDbContextCaches();
        var actorMessageQueueForEs = await actorMessageQueueRepository.ActorMessageQueueForAsync(receiverId, ActorRole.EnergySupplier);

        // The bundle should be removed from the queue for the energy supplier, but not for the grid operator.
        var dequeuedBundles = await bundleRepository.GetDequeuedBundlesOlderThanAsync(clockStub.GetCurrentInstant(), 100);
        dequeuedBundles.Should().NotContain(x => x.ActorMessageQueueId == actorMessageQueueForEs!.Id);

        // We are still able to peek the message for the grid operator.
        var peekResultForGo = await PeekMessageAsync(MessageCategory.Aggregations, chargeOwnerId, ActorRole.GridOperator);
        peekResultForGo.Should().NotBeNull();

        // blob should be cleaned up
        foreach (var outgoingMessage in outgoingMessages)
        {
            var downloadBlob = () => fileStorageClient.DownloadAsync(outgoingMessage!.FileStorageReference);
            await downloadBlob.Should().ThrowAsync<RequestFailedException>();
        }
    }
}
