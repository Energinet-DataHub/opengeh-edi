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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Application.Configuration.DataAccess;
using Application.OutgoingMessages.Peek;
using Dapper;
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.Peek;
using Infrastructure.OutgoingMessages;
using IntegrationTests.Application.IncomingMessages;
using IntegrationTests.Assertions;
using IntegrationTests.Factories;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;

namespace IntegrationTests.Application.OutgoingMessages;

public class WhenAPeekIsRequestedTests : TestBase
{
    private readonly BundledMessagesStub _bundledMessagesStub;
    private readonly MessagePeeker _messagePeeker;

    public WhenAPeekIsRequestedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _bundledMessagesStub = (BundledMessagesStub)GetService<IBundledMessages>();
        _messagePeeker = GetService<MessagePeeker>();
    }

    [Fact]
    public async Task When_no_messages_are_available_return_empty_result()
    {
        await GivenTwoMoveInTransactionHasBeenAccepted().ConfigureAwait(false);

        var result = await PeekMessage(MessageCategory.Aggregations).ConfigureAwait(false);

        Assert.Null(result.Bundle);
        Assert.False(await BundleIsRegistered().ConfigureAwait(false));
    }

    [Fact]
    public async Task A_message_bundle_is_returned()
    {
        await GivenTwoMoveInTransactionHasBeenAccepted();

        var result = await PeekMessage(MessageCategory.MasterData).ConfigureAwait(false);

        Assert.NotNull(result.Bundle);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.ConfirmRequestChangeOfSupplier)
            .IsProcesType(ProcessType.MoveIn)
            .HasMarketActivityRecordCount(2);
    }

    [Fact]
    public async Task Bundled_message_contains_maximum_number_of_payloads()
    {
        SetMaximumNumberOfPayloadsInBundle(1);
        await GivenTwoMoveInTransactionHasBeenAccepted().ConfigureAwait(false);
        await InsertFakeMessagesAsync(SampleData.NewEnergySupplierNumber, MarketRole.EnergySupplier, MessageCategory.MasterData, ProcessType.MoveIn, DocumentType.ConfirmRequestChangeOfSupplier).ConfigureAwait(false);

        var result = await PeekMessage(MessageCategory.MasterData).ConfigureAwait(false);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.ConfirmRequestChangeOfSupplier)
            .IsProcesType(ProcessType.MoveIn)
            .HasMarketActivityRecordCount(1);
    }

    [Fact]
    public async Task Ensure_same_bundle_is_returned_if_not_dequeued()
    {
        await GivenAMoveInTransactionHasBeenAccepted().ConfigureAwait(false);

        var firstPeekResult = await PeekMessage(MessageCategory.MasterData).ConfigureAwait(false);
        var secondPeekResult = await PeekMessage(MessageCategory.MasterData).ConfigureAwait(false);

        Assert.NotNull(firstPeekResult.MessageId);
        Assert.NotNull(secondPeekResult.MessageId);
        AssertXmlMessage.IsTheSameDocument(firstPeekResult.Bundle!, secondPeekResult.Bundle!);
    }

    [Fact]
    public async Task Return_empty_bundle_if_bundle_is_already_registered()
    {
        await GivenAMoveInTransactionHasBeenAccepted().ConfigureAwait(false);
        await PeekMessage(MessageCategory.MasterData).ConfigureAwait(false);

        _bundledMessagesStub.ReturnsEmptyMessage();
        var peekResult = await PeekMessage(MessageCategory.MasterData).ConfigureAwait(false);

        Assert.Null(peekResult.Bundle);
    }

    [Fact]
    public async Task The_generated_document_is_archived()
    {
        await GivenAMoveInTransactionHasBeenAccepted().ConfigureAwait(false);

        var result = await PeekMessage(MessageCategory.MasterData).ConfigureAwait(false);

        await AssertMessageIsArchived(result.MessageId);
    }

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId);
    }

    private async Task InsertFakeMessagesAsync(string receiverId, MarketRole receiverRole, MessageCategory category, ProcessType processType, DocumentType documentType)
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
                documentType.Name,
                category.Name,
                processType.Name,
                "MessageRecord");

            await messageEnqueuer.EnqueueAsync(message).ConfigureAwait(false);
        }
    }

    private async Task GivenAMoveInTransactionHasBeenAccepted()
    {
        var incomingMessage = MessageBuilder()
            .WithMarketEvaluationPointId(SampleData.MeteringPointNumber)
            .WithProcessType(ProcessType.MoveIn)
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
            .WithProcessType(ProcessType.MoveIn)
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
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var numberOfBundles = await connection
            .ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.BundledMessages").ConfigureAwait(false);
        return numberOfBundles == 1;
    }

    private Task<PeekResult> PeekMessage(MessageCategory category)
    {
        return _messagePeeker.PeekAsync(ActorNumber.Create(SampleData.NewEnergySupplierNumber), category, DocumentFormat.Xml);
    }

    private async Task AssertMessageIsArchived(Guid? messageId)
    {
        var sqlStatement =
            $"SELECT COUNT(*) FROM [dbo].[ArchivedMessages] WHERE Id = '{messageId}'";
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var found = await connection.ExecuteScalarAsync<bool>(sqlStatement).ConfigureAwait(false);
        Assert.True(found);
    }
}
