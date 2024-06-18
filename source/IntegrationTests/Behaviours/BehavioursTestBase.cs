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
using Azure.Messaging.ServiceBus;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using BuildingBlocks.Application.Extensions.Options;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
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
using Energinet.DataHub.EDI.IntegrationEvents.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Process.Interfaces;
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
    private readonly IntegrationTestFixture _integrationTestFixture;
    private ServiceCollection? _services;
    private bool _disposed;

    protected BehavioursTestBase(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
    {
        ArgumentNullException.ThrowIfNull(integrationTestFixture);

        _integrationTestFixture = integrationTestFixture;

        IntegrationTestFixture.CleanupDatabase();
        integrationTestFixture.CleanupFileStorage();
        _serviceBusSenderFactoryStub = new ServiceBusSenderFactoryStub();
        TestAggregatedTimeSeriesRequestAcceptedHandlerSpy = new TestAggregatedTimeSeriesRequestAcceptedHandlerSpy();
        InboxEventNotificationHandler = new TestNotificationHandlerSpy();
        _systemDateTimeProviderStub = new SystemDateTimeProviderStub();
        _dateTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];
        _serviceProvider = BuildServices(testOutputHelper);
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

    protected async Task<PeekResultDto?> WhenPeekMessageAsync(
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

    protected ServiceBusSenderSpy CreateServiceBusSenderSpy()
    {
        var serviceBusSenderSpy = new ServiceBusSenderSpy(MockServiceBusName);
        _serviceBusSenderFactoryStub.AddSenderSpy(serviceBusSenderSpy);

        return serviceBusSenderSpy;
    }

    protected (TServiceBusMessage Message, Guid ProcessId) AssertServiceBusMessage<TServiceBusMessage>(ServiceBusSenderSpy senderSpy, Func<BinaryData, TServiceBusMessage> parser)
        where TServiceBusMessage : IMessage
    {
        var assertResult = AssertServiceBusMessages(senderSpy, 1, parser);
        return assertResult.Single();
    }

    protected IList<(TServiceBusMessage Message, Guid ProcessId)> AssertServiceBusMessages<TServiceBusMessage>(ServiceBusSenderSpy senderSpy, int expectedCount, Func<BinaryData, TServiceBusMessage> parser)
        where TServiceBusMessage : IMessage
    {
        var sentMessages = senderSpy.MessagesSent
            .Where(m => m.Subject == typeof(TServiceBusMessage).Name)
            .ToList();

        sentMessages.Should().HaveCount(expectedCount);

        List<(TServiceBusMessage Message, Guid ProcessId)> messages = new();
        using var scope = new AssertionScope();
        foreach (var message in sentMessages)
        {
            message.Subject.Should().Be(typeof(TServiceBusMessage).Name);
            message.Body.Should().NotBeNull();
            message.ApplicationProperties.TryGetValue("ReferenceId", out var referenceId);
            referenceId.Should().NotBeNull();
            Guid.TryParse(referenceId!.ToString()!, out var processId).Should().BeTrue();

            var parsedMessage = parser(message.Body);
            parsedMessage.Should().NotBeNull();

            messages.Add((parsedMessage, processId));
        }

        return messages;
    }

    protected async Task GivenIntegrationEventReceived(IEventMessage @event)
    {
        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), @event.EventName, @event.EventMinorVersion, @event);

        using var serviceScope = _serviceProvider.CreateScope();
        await serviceScope.ServiceProvider.GetRequiredService<IIntegrationEventHandler>().HandleAsync(integrationEvent);
    }

    protected async Task<PeekResultDto?> WhenActorPeeksMessage(ActorNumber actorNumber, ActorRole actorRole, DocumentFormat documentFormat)
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

            if (peekResult is null)
                break;

            peekResults.Add(peekResult);
            await WhenActorDequeuesMessage(peekResult.MessageId.Value, actorNumber, actorRole);
        }

        return peekResults;
    }

    protected AmountPerChargeResultProducedV1 GivenAmountPerChargeResultProducedV1Event(Action<AmountPerChargeResultProducedV1EventBuilder> builder)
    {
        var eventBuilder = new AmountPerChargeResultProducedV1EventBuilder();

        builder(eventBuilder);

        return eventBuilder.Build();
    }

    protected T GetService<T>()
        where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    protected async Task InitializeProcess(ServiceBusMessage serviceBusMessage, string expectedSubject)
    {
        using var scope = _serviceProvider.CreateScope();
        // We have to manually process the service bus message, as there isn't a real service bus
        serviceBusMessage.Subject.Should().Be(expectedSubject);
        serviceBusMessage.Body.Should().NotBeNull();

        await scope.ServiceProvider.GetRequiredService<IProcessClient>().InitializeAsync(serviceBusMessage.Subject, serviceBusMessage.Body.ToArray());
        await ProcessInternalCommandsAsync();
    }

    protected async Task ProcessInternalCommandsAsync()
    {
        await ProcessBackgroundTasksAsync();

        if (_processContext.QueuedInternalCommands.Any(command => command.ProcessedDate == null))
        {
            await ProcessInternalCommandsAsync();
        }
    }

    private async Task WhenActorDequeuesMessage(string messageId, ActorNumber actorNumber, ActorRole actorRole)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var outgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        await outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(messageId, actorRole, actorNumber), CancellationToken.None);
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

    private ServiceProvider BuildServices(ITestOutputHelper testOutputHelper)
    {
        Environment.SetEnvironmentVariable("FEATUREFLAG_ACTORMESSAGEQUEUE", "true");
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", IntegrationTestFixture.DatabaseConnectionString);
        Environment.SetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING", _integrationTestFixture.AzuriteManager.BlobStorageConnectionString);

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{ServiceBusOptions.SectionName}:{nameof(ServiceBusOptions.ManageConnectionString)}"] = MockServiceBusName,
                    [$"{ServiceBusOptions.SectionName}:{nameof(ServiceBusOptions.ListenConnectionString)}"] = MockServiceBusName,
                    [$"{ServiceBusOptions.SectionName}:{nameof(ServiceBusOptions.SendConnectionString)}"] = MockServiceBusName,
                    [$"{EdiInboxOptions.SectionName}:{nameof(EdiInboxOptions.QueueName)}"] = MockServiceBusName,
                    [$"{WholesaleInboxOptions.SectionName}:{nameof(WholesaleInboxOptions.QueueName)}"] = MockServiceBusName,
                    [$"{IncomingMessagesQueueOptions.SectionName}:{nameof(IncomingMessagesQueueOptions.QueueName)}"] = MockServiceBusName,
                    ["IntegrationEvents:TopicName"] = "NotEmpty",
                    ["IntegrationEvents:SubscriptionName"] = "NotEmpty",

                    // Databricks
                    [nameof(DatabricksSqlStatementOptions.WorkspaceUrl)] = _integrationTestFixture.IntegrationTestConfiguration.DatabricksSettings.WorkspaceUrl,
                    [nameof(DatabricksSqlStatementOptions.WorkspaceToken)] = _integrationTestFixture.IntegrationTestConfiguration.DatabricksSettings.WorkspaceAccessToken,
                    [nameof(DatabricksSqlStatementOptions.WarehouseId)] = _integrationTestFixture.IntegrationTestConfiguration.DatabricksSettings.WarehouseId,

                    // => EDI views
                    [$"{EdiDatabricksOptions.SectionName}:{nameof(EdiDatabricksOptions.DatabaseName)}"] = _integrationTestFixture.DatabricksSchemaManager.SchemaName,
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
