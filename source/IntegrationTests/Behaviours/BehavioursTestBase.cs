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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using BuildingBlocks.Application.Extensions.Options;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2BApi.DataRetention;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Energinet.DataHub.Wholesale.Events.Infrastructure.IntegrationEvents;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours;

/// <summary>
///     - IntegrationTests
///         - IntegrationTests.EventBuilders
///             - AggregatedMeasureDataEventBuilder
///         - IntegrationTests.DocumentAsserters
///             - AggregatedMeasureDataDocumentXMLAsserter
///
///         - IntegrationTests.Behaviours (BehaviourTestBase)
///                    IntegrationEvent
///                         (classes)
///                         - GivenEnergyResultProducedV2
///                             (methods)
///                             - When_ActorPeeksDocument_Then_ActorCanPeekCorrectDocument
///                             - When_ActorPeeksDocument_Then_DelegatedActorCanPeekCorrectDocument
///                         - GivenMonthlyAmountPerChargeResultProducedV1
///                             (methods)
///                             - When_ActorPeeksDocument_Then_ActorCanPeekCorrectDocument
///                             - When_ActorPeeksDocument_Then_DelegatedActorCanPeekCorrectDocument
///                         - GivenAmountPerChargeResultProducedV1
///                             (methods)
///                             - When_ActorPeeksDocument_Then_ActorCanPeekCorrectDocument
///                             - When_ChargeOwnerPeeksDocument_Then_ChargeOwnerCanPeekCorrectDocument
///                             - When_ActorPeeksDocument_Then_DelegatedActorCanPeekCorrectDocument
///                      IncomingRequests|IncomingMessages
///
///       (Existing)
///       - IntegrationEvents.Application.Test
///             - WhenAggregatedMeasureDataReceived
///      -------------------------------------------------------
///                 Unit tests
///                     (folder)
///                     NotifyWholesaleServices
///                         (classes)
///                         - NotifyWholesaleServiceDocumentWriterTests
///                             (methods)
///                             - Given_ChargeTypeIsFeeAndAmountFieldIsMissing_When_CreateDocument_Then_ThrowException
///
///             **** Rule of thumb ****
///                 Given = // Arrange
///                 When = // Act
///                 Then  = // Assert
///
/// </summary>
[Collection("IntegrationTest")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "This is a test class")]
[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test class")]
public class BehavioursTestBase : IDisposable
{
    private const string MockServiceBusName = "mock-name";
    private readonly ServiceBusSenderFactoryStub _serviceBusSenderFactoryStub;
    private readonly ProcessContext _processContext;
    private readonly IncomingMessagesContext _incomingMessagesContext;
    private readonly SystemDateTimeProviderStub _systemDateTimeProviderStub;
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly DateTimeZone _dateTimeZone;
    private readonly ServiceProvider _serviceProvider;
    private ServiceCollection? _services;
    private bool _disposed;

    protected BehavioursTestBase(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
    {
        ArgumentNullException.ThrowIfNull(integrationTestFixture);
        IntegrationTestFixture.CleanupDatabase();
        integrationTestFixture.CleanupFileStorage();
        _serviceBusSenderFactoryStub = new ServiceBusSenderFactoryStub();
        TestAggregatedTimeSeriesRequestAcceptedHandlerSpy = new TestAggregatedTimeSeriesRequestAcceptedHandlerSpy();
        InboxEventNotificationHandler = new TestNotificationHandlerSpy();
        _systemDateTimeProviderStub = new SystemDateTimeProviderStub();
        _dateTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];
        _serviceProvider = BuildServices(integrationTestFixture.AzuriteManager.BlobStorageConnectionString, testOutputHelper);
        _processContext = GetService<ProcessContext>();
        _incomingMessagesContext = GetService<IncomingMessagesContext>();
        _authenticatedActor = GetService<AuthenticatedActor>();
        _authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(ActorNumber.Create("1234512345888"), Restriction.None));
    }

    private TestAggregatedTimeSeriesRequestAcceptedHandlerSpy TestAggregatedTimeSeriesRequestAcceptedHandlerSpy { get; }

    private TestNotificationHandlerSpy InboxEventNotificationHandler { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async Task<PeekResultDto> WhenPeekMessageAsync(
        MessageCategory category,
        ActorNumber actorNumber,
        ActorRole actorRole,
        DocumentFormat documentFormat)
    {
        using var serviceScope = _serviceProvider.CreateScope();
        var outgoingMessagesClient = serviceScope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();

        var result = await outgoingMessagesClient.PeekAndCommitAsync(
            new PeekRequestDto(
                actorNumber,
                category,
                actorRole,
                documentFormat),
            CancellationToken.None);

        return result;
    }

    protected void ClearDbContextCaches()
    {
        if (_services == null) throw new InvalidOperationException("ServiceCollection is not yet initialized");

        var dbContextServices = _services
            .Where(s => s.ServiceType.IsSubclassOf(typeof(DbContext)) || s.ServiceType == typeof(DbContext))
            .Select(s => (DbContext)_serviceProvider.GetService(s.ServiceType)!);

        foreach (var dbContext in dbContextServices)
            dbContext.ChangeTracker.Clear();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _processContext.Dispose();
        _incomingMessagesContext.Dispose();
        _serviceBusSenderFactoryStub.Dispose();
        _serviceProvider.Dispose();
        _disposed = true;
    }

    protected Task CreateActorIfNotExistAsync(CreateActorDto createActorDto)
    {
        return GetService<IMasterDataClient>().CreateActorIfNotExistAsync(createActorDto, CancellationToken.None);
    }

    protected Task GivenGridAreaOwnershipAsync(string gridArea, ActorNumber actorNumber)
    {
        return GetService<IMasterDataClient>()
            .UpdateGridAreaOwnershipAsync(
                new GridAreaOwnershipAssignedDto(
                    gridArea,
                    _systemDateTimeProviderStub.Now().Minus(Duration.FromDays(100)),
                    actorNumber,
                    0),
                CancellationToken.None);
    }

    protected async Task HavingReceivedInboxEventAsync(string eventType, IMessage eventPayload, Guid processId)
    {
        await GetService<IInboxEventReceiver>().
            ReceiveAsync(
                EventId.From(Guid.NewGuid()),
                eventType,
                processId,
                eventPayload.ToByteArray())
            .ConfigureAwait(false);

        await ProcessReceivedInboxEventsAsync().ConfigureAwait(false);
        await ProcessInternalCommandsAsync().ConfigureAwait(false);
    }

    protected void GivenAuthenticatedActorIs(ActorNumber actorNumber, ActorRole actorRole)
    {
        _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(actorNumber, Restriction.Owned, actorRole));
    }

    protected void GivenNowIs(int year, int month, int day)
    {
        GivenNowIs(
            new LocalDate(year, month, day)
                .AtMidnight()
                .InZoneStrictly(_dateTimeZone)
                .ToInstant());
    }

    protected void GivenNowIs(Instant now)
    {
        _systemDateTimeProviderStub.SetNow(now);
    }

    protected Instant GetNow()
    {
        return _systemDateTimeProviderStub.Now();
    }

    protected Instant CreateDateInstant(int year, int month, int day)
    {
        return new LocalDate(year, month, day)
            .AtMidnight()
            .InZoneStrictly(_dateTimeZone)
            .ToInstant();
    }

    protected async Task GivenDelegation(
        Actor delegatedBy,
        Actor delegatedTo,
        string gridAreaCode,
        ProcessType processType,
        Instant startsAt,
        Instant? stopsAt = null,
        int sequenceNumber = 0)
    {
        await GetService<IMasterDataClient>()
            .CreateProcessDelegationAsync(
                new ProcessDelegationDto(
                    sequenceNumber,
                    processType,
                    gridAreaCode,
                    startsAt,
                    stopsAt ?? startsAt.Plus(Duration.FromDays(365)),
                    delegatedBy,
                    delegatedTo),
                CancellationToken.None);
    }

    // protected Task<ResponseMessage> GivenRequestAggregatedMeasureDataJsonAsync(
    //     string senderActorNumber,
    //     string senderActorRole,
    //     (int Year, int Month, int Day) periodStart,
    //     (int Year, int Month, int Day) periodEnd,
    //     string? gridArea,
    //     string energySupplierActorNumber,
    //     string transactionId)
    // {
    //     return GetService<IIncomingMessageClient>()
    //         .RegisterAndSendAsync(
    //             RequestAggregatedMeasureDataRequestBuilder.GetStream(
    //                 senderActorNumber,
    //                 senderActorRole,
    //                 new LocalDate(periodStart.Year, periodStart.Month, periodStart.Day)
    //                     .AtMidnight()
    //                     .InZoneStrictly(_dateTimeZone)
    //                     .ToInstant()
    //                     .ToString(),
    //                 new LocalDate(periodEnd.Year, periodEnd.Month, periodEnd.Day)
    //                     .AtMidnight()
    //                     .InZoneStrictly(_dateTimeZone)
    //                     .ToInstant()
    //                     .ToString(),
    //                 gridArea,
    //                 energySupplierActorNumber,
    //                 transactionId),
    //             DocumentFormat.Json,
    //             IncomingDocumentType.RequestAggregatedMeasureData,
    //             CancellationToken.None);
    // }

    protected async Task GivenReceivedAggregatedMeasureDataRequest(
        DocumentFormat documentFormat,
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        MeteringPointType meteringPointType,
        SettlementMethod settlementMethod,
        (int Year, int Month, int Day) periodStart,
        (int Year, int Month, int Day) periodEnd,
        string? gridArea,
        ActorNumber? energySupplier,
        ActorNumber? balanceResponsibleParty,
        string transactionId)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();

        var incomingMessageStream = RequestAggregatedMeasureDataRequestBuilder.GetStream(
            documentFormat,
            senderActorNumber,
            senderActorRole,
            meteringPointType,
            settlementMethod,
            CreateDateInstant(periodStart.Year, periodStart.Month, periodStart.Day),
            CreateDateInstant(periodEnd.Year, periodEnd.Month, periodEnd.Day),
            gridArea,
            energySupplier,
            balanceResponsibleParty,
            transactionId);

        var response = await
            incomingMessageClient.RegisterAndSendAsync(
                incomingMessageStream,
                documentFormat,
                IncomingDocumentType.RequestAggregatedMeasureData,
                CancellationToken.None);

        using (new AssertionScope())
        {
            response.IsErrorResponse.Should().BeFalse("because the response should not have an error. Actual response: {0}", response.MessageBody);
            response.MessageBody.Should().BeEmpty();
        }
    }

    protected async Task GivenReceivedWholesaleServicesRequest(
        DocumentFormat documentFormat,
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        (int Year, int Month, int Day) periodStart,
        (int Year, int Month, int Day) periodEnd,
        string? gridArea,
        ActorNumber energySupplierActorNumber,
        ActorNumber chargeOwnerActorNumber,
        string chargeCode,
        ChargeType chargeType,
        string transactionId,
        bool isMonthly)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();

        var incomingMessageStream = RequestWholesaleServicesRequestBuilder.GetStream(
            documentFormat,
            senderActorNumber,
            senderActorRole,
            CreateDateInstant(periodStart.Year, periodStart.Month, periodStart.Day),
            CreateDateInstant(periodEnd.Year, periodEnd.Month, periodEnd.Day),
            gridArea,
            energySupplierActorNumber,
            chargeOwnerActorNumber,
            chargeCode,
            chargeType,
            transactionId,
            isMonthly);

        var response = await
            incomingMessageClient.RegisterAndSendAsync(
                incomingMessageStream,
                documentFormat,
                IncomingDocumentType.RequestWholesaleSettlement,
                CancellationToken.None);

        using (new AssertionScope())
        {
            response.IsErrorResponse.Should().BeFalse();
            response.MessageBody.Should().BeEmpty();
        }
    }

    protected async Task GivenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(
        ServiceBusMessage serviceBusMessage)
    {
        // We have to manually process the service bus message, as there isn't a real service bus
        serviceBusMessage.Subject.Should().Be(nameof(InitializeAggregatedMeasureDataProcessDto));
        serviceBusMessage.Body.Should().NotBeNull();

        await GetService<IProcessClient>().InitializeAsync(serviceBusMessage.Subject, serviceBusMessage.Body.ToArray());
        await ProcessInternalCommandsAsync();
    }

    protected async Task WhenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(
        ServiceBusMessage serviceBusMessage)
    {
        await GivenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(serviceBusMessage);
    }

    protected async Task GivenWholesaleAcceptedResponseToAggregatedMeasureDataRequestAsync(
        ServiceBusMessage serviceBusMessage)
    {
        serviceBusMessage.Subject.Should().Be(nameof(AggregatedTimeSeriesRequest));
        serviceBusMessage.Body.Should().NotBeNull();

        var aggregatedTimeSeriesRequest =
            AggregatedTimeSeriesRequest.Parser.ParseFrom(serviceBusMessage.Body);

        var aggregatedTimeSeriesRequestAccepted =
            AggregatedTimeSeriesResponseEventBuilder.GenerateAcceptedFrom(aggregatedTimeSeriesRequest);

        await HavingReceivedInboxEventAsync(
            nameof(AggregatedTimeSeriesRequestAccepted),
            aggregatedTimeSeriesRequestAccepted,
            Guid.Parse(serviceBusMessage.MessageId));
    }

    protected ServiceBusSenderSpy CreateServiceBusSenderSpy()
    {
        var serviceBusSenderSpy = new ServiceBusSenderSpy(MockServiceBusName);
        _serviceBusSenderFactoryStub.AddSenderSpy(serviceBusSenderSpy);

        return serviceBusSenderSpy;
    }

    protected async Task WhenWholesaleServicesProcessIsInitialized(ServiceBusMessage serviceBusMessage)
    {
        await InitializeProcess(serviceBusMessage, nameof(InitializeWholesaleServicesProcessDto));
    }

    protected async Task WhenAggregatedMeasureDataProcessIsInitialized(ServiceBusMessage serviceBusMessage)
    {
        await InitializeProcess(serviceBusMessage, nameof(InitializeAggregatedMeasureDataProcessDto));
    }

    protected (TServiceBusMessage Message, Guid ProcessId) AssertServiceBusMessage<TServiceBusMessage>(ServiceBusSenderSpy senderSpy, Func<BinaryData, TServiceBusMessage> parser)
        where TServiceBusMessage : IMessage
    {
        using (new AssertionScope())
        {
            senderSpy.MessageSent.Should().BeTrue();
            senderSpy.Message.Should().NotBeNull();
        }

        var serviceBusMessage = senderSpy.Message!;
        Guid processId;
        using (new AssertionScope())
        {
            serviceBusMessage.Subject.Should().Be(typeof(TServiceBusMessage).Name);
            serviceBusMessage.Body.Should().NotBeNull();
            serviceBusMessage.ApplicationProperties.TryGetValue("ReferenceId", out var referenceId);
            referenceId.Should().NotBeNull();
            Guid.TryParse(referenceId!.ToString()!, out processId).Should().BeTrue();
        }

        var parsedMessage = parser(serviceBusMessage.Body);
        // var aggregatedMeasureDataRequestMessage = AggregatedTimeSeriesRequest.Parser.ParseFrom(serviceBusMessage.Body);
        parsedMessage.Should().NotBeNull();

        return (parsedMessage, processId);
    }

    protected async Task GivenIntegrationEventReceived(IEventMessage @event)
    {
        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), @event.EventName, @event.EventMinorVersion, @event);

        using var serviceScope = _serviceProvider.CreateScope();
        await serviceScope.ServiceProvider.GetRequiredService<IIntegrationEventHandler>().HandleAsync(integrationEvent);
    }

    protected async Task<PeekResultDto> WhenActorPeeksMessage(ActorNumber actorNumber, ActorRole actorRole, DocumentFormat documentFormat)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var outgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var peekResult = await outgoingMessagesClient.PeekAndCommitAsync(new PeekRequestDto(actorNumber, MessageCategory.Aggregations, actorRole, documentFormat), CancellationToken.None);
        return peekResult;
    }

    protected async Task<List<PeekResultDto>> WhenActorPeeksAllMessages(ActorNumber actorNumber, ActorRole actorRole, DocumentFormat documentFormat)
    {
        var peekResults = new List<PeekResultDto>();

        var timeoutAt = DateTime.UtcNow.AddMinutes(1);
        while (DateTime.UtcNow < timeoutAt)
        {
            var peekResult = await WhenActorPeeksMessage(actorNumber, actorRole, documentFormat);

            if (peekResult.MessageId == null)
                break;

            peekResults.Add(peekResult);
            await WhenActorDequeuesMessage(peekResult.MessageId.ToString()!, actorNumber, actorRole);
        }

        return peekResults;
    }

    protected async Task ThenNotifyWholesaleServicesDocumentIsCorrect(
        Stream? peekResultDocumentStream,
        DocumentFormat documentFormat,
        NotifyWholesaleServicesDocumentAssertionInput assertionInput)
    {
        peekResultDocumentStream.Should().NotBeNull();
        peekResultDocumentStream!.Position = 0;

        using var assertionScope = new AssertionScope();

        await NotifyWholesaleServicesDocumentAsserter.AssertCorrectDocumentAsync(
            documentFormat,
            peekResultDocumentStream,
            assertionInput);
    }

    protected async Task ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
        Stream? peekResultDocumentStream,
        DocumentFormat documentFormat,
        NotifyAggregatedMeasureDataDocumentAssertionInput assertionInput)
    {
        peekResultDocumentStream.Should().NotBeNull();
        peekResultDocumentStream!.Position = 0;

        using var assertionScope = new AssertionScope();

        await NotifyAggregatedMeasureDataDocumentAsserter.AssertCorrectDocumentAsync(
            documentFormat,
            peekResultDocumentStream,
            assertionInput);
    }

    protected async Task ThenRejectRequestWholesaleSettlementDocumentIsCorrect(Stream? peekResultDocumentStream, DocumentFormat documentFormat, RejectRequestWholesaleSettlementDocumentAssertionInput assertionInput)
    {
        peekResultDocumentStream.Should().NotBeNull();
        peekResultDocumentStream!.Position = 0;

        using var assertionScope = new AssertionScope();

        await RejectRequestWholesaleSettlementDocumentAsserter.AssertCorrectDocumentAsync(
            documentFormat,
            peekResultDocumentStream,
            assertionInput);
    }

    protected AmountPerChargeResultProducedV1 GivenAmountPerChargeResultProducedV1Event(Action<AmountPerChargeResultProducedV1EventBuilder> builder)
    {
        var eventBuilder = new AmountPerChargeResultProducedV1EventBuilder();

        builder(eventBuilder);

        return eventBuilder.Build();
    }

    private async Task InitializeProcess(ServiceBusMessage serviceBusMessage, string expectedSubject)
    {
        using var scope = _serviceProvider.CreateScope();
        // We have to manually process the service bus message, as there isn't a real service bus
        serviceBusMessage.Subject.Should().Be(expectedSubject);
        serviceBusMessage.Body.Should().NotBeNull();

        await scope.ServiceProvider.GetRequiredService<IProcessClient>().InitializeAsync(serviceBusMessage.Subject, serviceBusMessage.Body.ToArray());
        await ProcessInternalCommandsAsync();
    }

    private async Task WhenActorDequeuesMessage(string messageId, ActorNumber actorNumber, ActorRole actorRole)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var outgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        await outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(messageId, actorRole, actorNumber), CancellationToken.None);
    }

    private async Task ProcessInternalCommandsAsync()
    {
        await ProcessBackgroundTasksAsync();

        if (_processContext.QueuedInternalCommands.Any(command => command.ProcessedDate == null))
        {
            await ProcessInternalCommandsAsync();
        }
    }

    private T GetService<T>()
        where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    private Task ProcessReceivedInboxEventsAsync()
    {
        return ProcessBackgroundTasksAsync();
    }

    private async Task ProcessBackgroundTasksAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var datetimeProvider = scope.ServiceProvider.GetRequiredService<ISystemDateTimeProvider>();
        await scope.ServiceProvider
            .GetRequiredService<IMediator>()
            .Publish(new TenSecondsHasHasPassed(datetimeProvider.Now()));
    }

    private ServiceProvider BuildServices(string fileStorageConnectionString, ITestOutputHelper testOutputHelper)
    {
        Environment.SetEnvironmentVariable("FEATUREFLAG_ACTORMESSAGEQUEUE", "true");
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", IntegrationTestFixture.DatabaseConnectionString);
        Environment.SetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING", fileStorageConnectionString);

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{ServiceBusOptions.SectionName}:{nameof(ServiceBusOptions.ListenConnectionString)}"] = MockServiceBusName,
                    [$"{ServiceBusOptions.SectionName}:{nameof(ServiceBusOptions.SendConnectionString)}"] = MockServiceBusName,
                    [$"{EdiInboxOptions.SectionName}:{nameof(EdiInboxOptions.QueueName)}"] = MockServiceBusName,
                    [$"{WholesaleInboxOptions.SectionName}:{nameof(WholesaleInboxOptions.QueueName)}"] = MockServiceBusName,
                    [$"{IncomingMessagesQueueOptions.SectionName}:{nameof(IncomingMessagesQueueOptions.QueueName)}"] = MockServiceBusName,
                    ["IntegrationEvents:TopicName"] = "NotEmpty",
                    ["IntegrationEvents:SubscriptionName"] = "NotEmpty",
                })
            .Build();

        _services = new ServiceCollection();
        _services.AddScoped<IConfiguration>(_ => config);

        _services.AddTransient<InboxEventsProcessor>()
            .AddTransient<INotificationHandler<AggregatedTimeSeriesRequestWasAccepted>>(
                _ => TestAggregatedTimeSeriesRequestAcceptedHandlerSpy)
            .AddTransient<INotificationHandler<TestNotification>>(_ => InboxEventNotificationHandler)
            .AddTransient<IRequestHandler<TestCommand, Unit>, TestCommandHandler>()
            .AddTransient<IRequestHandler<TestCreateOutgoingMessageCommand, Unit>,
                TestCreateOutgoingCommandHandler>()
            .AddScopedSqlDbContext<ProcessContext>(config)
            .AddB2BAuthentication(JwtTokenParserTests.DisableAllTokenValidations)
            .AddSerializer()
            .AddLogging()
            .AddScoped<ISystemDateTimeProvider>(_ => _systemDateTimeProviderStub);

        _services.AddTransient<INotificationHandler<ADayHasPassed>, ExecuteDataRetentionsWhenADayHasPassed>()
            .AddIntegrationEventModule(config)
            .AddOutgoingMessagesModule(config)
            .AddProcessModule(config)
            .AddArchivedMessagesModule(config)
            .AddIncomingMessagesModule(config)
            .AddMasterDataModule(config)
            .AddDataAccessUnitOfWorkModule(config);

        _services.AddScoped<Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext>((x) =>
        {
            var executionContext = new Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext();
            executionContext.SetExecutionType(ExecutionType.Test);
            return executionContext;
        });

        // Replace the services with stub implementations.
        // - Building blocks
        _services.AddSingleton<IServiceBusSenderFactory>(_serviceBusSenderFactoryStub);
        _services.AddTransient<IFeatureFlagManager>(_ => new FeatureFlagManagerStub());

        // Add test logger
        _services.AddSingleton<ITestOutputHelper>(sp => testOutputHelper);
        _services.Add(ServiceDescriptor.Singleton(typeof(Logger<>), typeof(Logger<>)));
        _services.Add(ServiceDescriptor.Transient(typeof(ILogger<>), typeof(TestLogger<>)));

        return _services.BuildServiceProvider();
    }
}
