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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
using Energinet.DataHub.EDI.Api.IncomingMessages;
using Energinet.DataHub.EDI.Api.OutgoingMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Api.Mocks;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Api.Mwo;

public sealed class MwoScienceTests : TestBase
{
    public MwoScienceTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
    }

    [Fact]
    public async Task Science_In()
    {
        var serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        var senderSpy = new ServiceBusSenderSpy("Fake");
        serviceBusClientSenderFactory.AddSenderSpy(senderSpy);

        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(ActorNumber.Create("5799999933318"), Restriction.None, ActorRole.FromCode("DDK")));

        var sut = new IncomingMessageReceiver(
            GetService<ILogger<IncomingMessageReceiver>>(),
            GetService<IIncomingMessageClient>(),
            GetService<ICorrelationContext>(),
            GetService<IFeatureFlagManager>());

        var functionContext = new FunctionContextBuilder(ServiceProvider)
            .WithBodyHack(
                """
                {
                    "RequestAggregatedMeasureData_MarketDocument": {
                        "mRID": "123564789123564789123564789123564789",
                        "businessSector.type": {
                            "value": "23"
                        },
                        "createdDateTime": "2022-12-17T09:30:47Z",
                        "process.processType": {
                            "value": "D05"
                        },
                        "receiver_MarketParticipant.mRID": {
                            "codingScheme": "A10",
                            "value": "5790001330552"
                        },
                        "receiver_MarketParticipant.marketRole.type": {
                            "value": "DGL"
                        },
                        "sender_MarketParticipant.mRID": {
                            "codingScheme": "A10",
                            "value": "5799999933318"
                        },
                        "sender_MarketParticipant.marketRole.type": {
                            "value": "DDK"
                        },
                        "type": {
                            "value": "E74"
                        },
                        "Series": [
                            {
                                "mRID": "123564789123564789123564789123564787",
                                "balanceResponsibleParty_MarketParticipant.mRID": {
                                    "codingScheme": "A10",
                                    "value": "5799999933318"
                                },
                                "end_DateAndOrTime.dateTime": "2022-07-22T22:00:00Z",
                                "energySupplier_MarketParticipant.mRID": {
                                    "codingScheme": "A10",
                                    "value": "5799999933318"
                                },
                                "marketEvaluationPoint.settlementMethod": {
                                    "value": "D01"
                                },
                                "marketEvaluationPoint.type": {
                                    "value": "E17"
                                },
                                "meteringGridArea_Domain.mRID": {
                                    "codingScheme": "NDK",
                                    "value": "244"
                                },
                                "start_DateAndOrTime.dateTime": "2022-06-17T22:00:00Z",
                                "settlement_Series.version": {
                                    "value": "D01"
                                }
                            }
                        ]
                    }
                }
                """)
            .Build();

        var httpRequestData =
            await functionContext.Features.Get<IHttpRequestDataFeature>()!.GetHttpRequestDataAsync(functionContext);

        var response = await sut.RunAsync(
            httpRequestData!,
            functionContext,
            nameof(IncomingDocumentType.RequestAggregatedMeasureData),
            CancellationToken.None);

        response.Should().NotBeNull();
        response.Body.ToString().Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        (await reader.ReadToEndAsync()).Should().BeEmpty();

        senderSpy.Message.Should().NotBeNull();
        senderSpy.Message!.Subject.Should().Be(nameof(RequestAggregatedMeasureDataDto));
    }

    [Fact]
    public async Task Science_Out()
    {
        // GET THE DATA FROM WHOLESALE
        var eventBuilder = new EnergyResultProducedV2EventBuilder()
            .WithCalculationType(EnergyResultProducedV2.Types.CalculationType.BalanceFixing)
            .AggregatedBy("512", "5799999933318")
            .ResultOf(EnergyResultProducedV2.Types.TimeSeriesType.NonProfiledConsumption)
            .WithResolution(EnergyResultProducedV2.Types.Resolution.Quarter);

        var integrationEventHandler = GetService<IIntegrationEventHandler>();
        var integrationEvent = new IntegrationEvent(
            Guid.NewGuid(),
            EnergyResultProducedV2.EventName,
            1,
            eventBuilder.Build());

        await integrationEventHandler.HandleAsync(integrationEvent);
        // WE GOT THE DATA FROM WHOLESALE

        var serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        var senderSpy = new ServiceBusSenderSpy("Fake");
        serviceBusClientSenderFactory.AddSenderSpy(senderSpy);

        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(ActorNumber.Create("5799999933318"), Restriction.None, ActorRole.FromCode("DDK")));

        var sut = new PeekRequestListener(
            GetService<AuthenticatedActor>(),
            GetService<ILogger<PeekRequestListener>>(),
            GetService<IOutgoingMessagesClient>());

        var functionContext = new FunctionContextBuilder(ServiceProvider)
            .WithBodyHack(string.Empty)
            .Build();

        var httpRequestData =
            await functionContext.Features.Get<IHttpRequestDataFeature>()!.GetHttpRequestDataAsync(functionContext);

        var response = await sut.RunAsync(
            httpRequestData!,
            functionContext,
            nameof(MessageCategory.Aggregations),
            CancellationToken.None);

        response.Should().NotBeNull();
        response.Body.ToString().Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        (await reader.ReadToEndAsync()).Should().NotBeEmpty().And.Contain("5799999933318");
    }
}
