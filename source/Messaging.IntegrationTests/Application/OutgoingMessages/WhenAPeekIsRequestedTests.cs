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

using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Infrastructure.OutgoingMessages;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Factories;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;

namespace Messaging.IntegrationTests.Application.OutgoingMessages;

public class WhenAPeekIsRequestedTests : TestBase
{
    public WhenAPeekIsRequestedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task When_no_messages_are_available_return_empty_result()
    {
        await GivenTwoMoveInTransactionHasBeenAccepted().ConfigureAwait(false);

        var result = await InvokeCommandAsync(CreatePeekRequest(MessageCategory.Aggregations)).ConfigureAwait(false);

        Assert.Null(result.Bundle);
        Assert.False(await BundleIsRegistered().ConfigureAwait(false));
    }

    [Fact]
    public async Task A_message_bundle_is_returned()
    {
        await GivenTwoMoveInTransactionHasBeenAccepted();

        var command = CreatePeekRequest(MessageCategory.MasterData);
        var result = await InvokeCommandAsync(command).ConfigureAwait(false);

        Assert.NotNull(result.Bundle);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(MessageType.ConfirmRequestChangeOfSupplier)
            .IsProcesType(ProcessType.MoveIn)
            .HasMarketActivityRecordCount(2);
    }

    [Fact]
    public async Task Bundled_message_contains_maximum_number_of_payloads()
    {
        SetMaximumNumberOfPayloadsInBundle(1);
        await GivenTwoMoveInTransactionHasBeenAccepted().ConfigureAwait(false);
        await InsertFakeMessagesAsync(SampleData.NewEnergySupplierNumber, MarketRole.EnergySupplier, MessageCategory.MasterData, ProcessType.MoveIn, MessageType.ConfirmRequestChangeOfSupplier).ConfigureAwait(false);

        var command = CreatePeekRequest(MessageCategory.MasterData);
        var result = await InvokeCommandAsync(command).ConfigureAwait(false);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(MessageType.ConfirmRequestChangeOfSupplier)
            .IsProcesType(ProcessType.MoveIn)
            .HasMarketActivityRecordCount(1);
    }

    [Fact]
    public async Task Ensure_same_bundle_is_returned_if_not_dequeued()
    {
        await GivenAMoveInTransactionHasBeenAccepted().ConfigureAwait(false);

        var command = CreatePeekRequest(MessageCategory.MasterData);
        var firstPeekResult = await InvokeCommandAsync(command).ConfigureAwait(false);
        var secondPeekResult = await InvokeCommandAsync(command).ConfigureAwait(false);

        Assert.NotNull(firstPeekResult.MessageId);
        Assert.NotNull(secondPeekResult.MessageId);
        AssertXmlMessage.IsTheSameDocument(firstPeekResult.Bundle!, secondPeekResult.Bundle!);
    }

    [Fact]
    public async Task Return_no_content_if_bundling_is_in_progress()
    {
        await SimulateThatBundlingIsAlreadyInProgress().ConfigureAwait(false);

        var peekResult = await InvokeCommandAsync(CreatePeekRequest(MessageCategory.MasterData)).ConfigureAwait(false);

        Assert.Null(peekResult.Bundle);
    }

    private static PeekRequest CreatePeekRequest(MessageCategory messageCategory)
    {
        return new PeekRequest(ActorNumber.Create(SampleData.NewEnergySupplierNumber), messageCategory);
    }

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId);
    }

    private async Task InsertFakeMessagesAsync(string receiverId, MarketRole receiverRole, MessageCategory category, ProcessType processType, MessageType messageType)
    {
        var messageEnqueuer = GetService<OutgoingMessageEnqueuer>();

        for (var i = 0; i < 3; i++)
        {
            var message = new EnqueuedMessage(
                Guid.NewGuid(),
                receiverId,
                receiverRole.Name,
                Guid.NewGuid().ToString(),
                "FakeSenderRole",
                messageType.Name,
                category.Name,
                processType.Code,
                "MessageRecord");

            await messageEnqueuer.EnqueueAsync(message).ConfigureAwait(false);
        }
    }

    private async Task SimulateThatBundlingIsAlreadyInProgress()
    {
        await GetService<BundleStore>()
            .TryRegisterBundleAsync(
                BundleId.Create(
                    MessageCategory.MasterData,
                    ActorNumber.Create(SampleData.NewEnergySupplierNumber)))
            .ConfigureAwait(false);
    }

    private async Task GivenAMoveInTransactionHasBeenAccepted()
    {
        var incomingMessage = MessageBuilder()
            .WithMarketEvaluationPointId(SampleData.MeteringPointNumber)
            .WithProcessType(ProcessType.MoveIn.Code)
            .WithReceiver(SampleData.ReceiverId)
            .WithSenderId(SampleData.SenderId)
            .WithConsumerName(SampleData.ConsumerName)
            .Build();

        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
    }

    private async Task GivenTwoMoveInTransactionHasBeenAccepted()
    {
        await GivenAMoveInTransactionHasBeenAccepted().ConfigureAwait(false);

        var message = MessageBuilder()
            .WithProcessType(ProcessType.MoveIn.Code)
            .WithReceiver(SampleData.ReceiverId)
            .WithSenderId(SampleData.SenderId)
            .WithEffectiveDate(EffectiveDateFactory.OffsetDaysFromToday(1))
            .WithConsumerId(ConsumerFactory.CreateConsumerId())
            .WithConsumerName(ConsumerFactory.CreateConsumerName())
            .WithTransactionId(Guid.NewGuid().ToString()).Build();

        await InvokeCommandAsync(message).ConfigureAwait(false);
    }

    private void SetMaximumNumberOfPayloadsInBundle(int maxNumberOfPayloadsInBundle)
    {
        var bundleConfiguration = (BundleConfigurationStub)GetService<IBundleConfiguration>();
        bundleConfiguration.MaxNumberOfPayloadsInBundle = maxNumberOfPayloadsInBundle;
    }

    private async Task<bool> BundleIsRegistered()
    {
        using var connection = await GetService<IEdiDatabaseConnection>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var numberOfBundles = await connection
            .ExecuteScalarAsync<int>("SELECT COUNT(*) FROM b2b.BundleStore").ConfigureAwait(false);
        return numberOfBundles == 1;
    }
}
