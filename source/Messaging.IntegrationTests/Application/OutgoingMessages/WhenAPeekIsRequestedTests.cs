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
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Infrastructure.Transactions;
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

        var result = await InvokeCommandAsync(CreatePeekRequest(MessageCategory.AggregationData)).ConfigureAwait(false);

        Assert.Null(result.Bundle);
    }

    [Fact]
    public async Task A_message_bundle_is_returned()
    {
        await GivenTwoMoveInTransactionHasBeenAccepted();

        var command = CreatePeekRequest(MessageCategory.MasterData);
        var result = await InvokeCommandAsync(command).ConfigureAwait(false);

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

        var command = CreatePeekRequest(MessageCategory.MasterData);
        var result = await InvokeCommandAsync(command).ConfigureAwait(false);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.ConfirmRequestChangeOfSupplier)
            .IsProcesType(ProcessType.MoveIn)
            .HasMarketActivityRecordCount(1);
    }

    [Fact]
    public async Task Bundled_message_contains_payloads_for_the_requested_receiver_role()
    {
        var actor = SampleData.NewEnergySupplierNumber;

        await CreateActorMessageQueueTableAsync(actor).ConfigureAwait(false);
        await InsertFakeBusinessProcessDataAsync(actor, MarketRole.EnergySupplier).ConfigureAwait(false);
        await GivenMoveInHasCompleted().ConfigureAwait(false);
        await InsertFakeBusinessProcessDataAsync(actor, MarketRole.EnergySupplier).ConfigureAwait(false);

        var command = CreatePeekRequest(MessageCategory.MasterData);
        var result = await InvokeCommandAsync(command).ConfigureAwait(false);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.ConfirmRequestChangeOfSupplier)
            .IsProcesType(ProcessType.MoveIn)
            .HasReceiverRole(MarketRole.EnergySupplier)
            .HasMarketActivityRecordCount(1);
    }

    private static PeekRequest CreatePeekRequest(MessageCategory messageCategory)
    {
        return new PeekRequest(ActorNumber.Create(SampleData.NewEnergySupplierNumber), messageCategory, MarketRole.EnergySupplier);
    }

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId);
    }

    private Task CreateActorMessageQueueTableAsync(string actor)
    {
        var sql = @$"CREATE TABLE [B2B].ActorMessageQueue_{actor}(
        [RecordId]                        [int] IDENTITY (1,1) NOT NULL,
        [Id]                              [uniqueIdentifier] NOT NULL,
        [DocumentType]                    [VARCHAR](255)     NOT NULL,
        [MessageCategory]                 [VARCHAR](255)     NOT NULL,
        [ReceiverId]                      [VARCHAR](255)     NOT NULL,
        [ReceiverRole]                    [VARCHAR](50)      NOT NULL,
        [SenderId]                        [VARCHAR](255)     NOT NULL,
        [SenderRole]                      [VARCHAR](50)      NOT NULL,
        [ProcessType]                     [VARCHAR](50)      NOT NULL,
        [Payload]                         [NVARCHAR](MAX)    NOT NULL,
        CONSTRAINT [PK_ActorMessageQueue_{actor}_Id] PRIMARY KEY NONCLUSTERED
                (
            [Id] ASC
            ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
            ) ON [PRIMARY]";

        return GetService<IDbConnectionFactory>().GetOpenConnection().ExecuteAsync(sql);
    }

    private async Task InsertFakeBusinessProcessDataAsync(string actor, MarketRole receiverRole)
    {
        var sql = @$"INSERT INTO [B2B].[ActorMessageQueue_{actor}] VALUES (@Id, @DocumentType, @MessageCategory, @ReceiverId, @ReceiverRole, @SenderId, @SenderRole, @ProcessType, @Payload)";

        for (var i = 0; i < 3; i++)
        {
            await GetService<IDbConnectionFactory>().GetOpenConnection().ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                DocumentType = "FakeDocumentType",
                MessageCategory = "FakeMessageCategory",
                ReceiverId = SampleData.NewEnergySupplierNumber,
                ReceiverRole = receiverRole.Name,
                SenderId = Guid.NewGuid().ToString(),
                SenderRole = "FakeSenderRole",
                ProcessType = "FakeBusinessProcess",
                Payload = "Payload",
            }).ConfigureAwait(false);
        }
    }

    private async Task GivenMoveInHasCompleted()
    {
        await new TestDataBuilder(this)
            .AddActor(SampleData.GridOperatorId, SampleData.GridOperatorNumber)
            .AddMarketEvaluationPoint(
                Guid.Parse(SampleData.MarketEvaluationPointId),
                SampleData.GridOperatorId,
                SampleData.MeteringPointNumber)
            .BuildAsync().ConfigureAwait(false);

        var httpClientAdapter = (HttpClientSpy)GetService<IHttpClientAdapter>();
        httpClientAdapter.RespondWithBusinessProcessId(SampleData.BusinessProcessId);

        await GivenAMoveInTransactionHasBeenAccepted().ConfigureAwait(false);
        await InvokeCommandAsync(new SetConsumerHasMovedIn(SampleData.BusinessProcessId.ToString())).ConfigureAwait(false);
        await SimulateTenSecondsHasPassedAsync().ConfigureAwait(false);
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
}
