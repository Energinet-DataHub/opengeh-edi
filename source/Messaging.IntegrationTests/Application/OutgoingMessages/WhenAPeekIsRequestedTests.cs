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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediatR;
using Messaging.Application.Actors;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Application.Configuration.TimeEvents;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Domain.Actors;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Infrastructure.Configuration.DataAccess;
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
        var command = CreatePeekRequest(MessageCategory.AggregationData);

        var result = await InvokeCommandAsync(command).ConfigureAwait(false);

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
        var scenario = new ScenarioSetup(this);

        var gridOperatorId = Guid.NewGuid();
        await scenario
            .HasActor(gridOperatorId, SampleData.NewEnergySupplierNumber)
            .HasMarketEvaluationPoint(
                Guid.Parse(SampleData.MarketEvaluationPointId),
                gridOperatorId,
                SampleData.MeteringPointNumber)
            .BuildAsync().ConfigureAwait(false);
        var httpClientAdapter = (HttpClientSpy)GetService<IHttpClientAdapter>();
        httpClientAdapter.RespondWithBusinessProcessId(SampleData.BusinessProcessId);

        await GivenAMoveInTransactionHasBeenAccepted().ConfigureAwait(false);
        await InvokeCommandAsync(new SetConsumerHasMovedIn(SampleData.BusinessProcessId.ToString()));
        var mediator = GetService<IMediator>();
        await mediator.Publish(new TenSecondsHasHasPassed(GetService<ISystemDateTimeProvider>().Now()));

        var command = CreatePeekRequest(MessageCategory.MasterData);
        var result = await InvokeCommandAsync(command).ConfigureAwait(false);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.ConfirmRequestChangeOfSupplier)
            .IsProcesType(ProcessType.MoveIn)
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

#pragma warning disable
public class ScenarioSetup
{
    private readonly TestBase _testBase;
    private readonly List<object> _commands = new();

    public ScenarioSetup(TestBase testBase)
    {
        _testBase = testBase;

    }

    public ScenarioSetup HasActor(Guid actorId, string actorNumber)
    {
        _commands.Add(new CreateActor(actorId.ToString(), Guid.NewGuid().ToString(), actorNumber));
        return this;
    }

    public ScenarioSetup HasMarketEvaluationPoint(Guid marketEvaluationPointId, Guid gridOperatorId, string marketEvaluationPointNumber)
    {
        var marketEvaluationPoint = new MarketEvaluationPoint(
            marketEvaluationPointId,
            marketEvaluationPointNumber);
        marketEvaluationPoint.SetGridOperatorId(gridOperatorId);
        var b2BContext = _testBase.GetService<B2BContext>();
        b2BContext.MarketEvaluationPoints.Add(marketEvaluationPoint);

        return this;
    }



    public async Task BuildAsync()
    {
        foreach (var command in _commands)
        {
            await _testBase.InvokeCommandAsync(command).ConfigureAwait(false);
        }

        var b2BContext = _testBase.GetService<B2BContext>();
        if(b2BContext.ChangeTracker.HasChanges())
            await b2BContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
