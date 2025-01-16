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
using System.Text;
using Azure.Messaging.ServiceBus;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Extensions;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.IncomingMessages;

[SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1118:Parameter should not span multiple lines",
    Justification = "Readability in test-setup")]
public sealed class GivenIncomingMessagesTests : IncomingMessagesTestBase
{
    private readonly IIncomingMessageClient _incomingMessagesRequest;
#pragma warning disable CA2213 // Disposable fields should be disposed
    private readonly ServiceBusSenderSpy _senderSpy;
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly IncomingMessagesContext _incomingMessageContext;
    private readonly ClockStub _clockStub;

    public GivenIncomingMessagesTests(
        IncomingMessagesTestFixture incomingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(incomingMessagesTestFixture, testOutputHelper)
    {
        _senderSpy = new ServiceBusSenderSpy("Fake");
        var serviceBusClientSenderFactory =
            (ServiceBusSenderFactoryStub)GetService<IAzureClientFactory<ServiceBusSender>>();

        serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
        _incomingMessagesRequest = GetService<IIncomingMessageClient>();
        _incomingMessageContext = GetService<IncomingMessagesContext>();
        _clockStub = (ClockStub)GetService<IClock>();
    }

    public static TheoryData<DocumentFormat, IncomingDocumentType, ActorRole, IncomingMarketMessageStream> ValidIncomingRequestMessages()
    {
        var data = new TheoryData<DocumentFormat, IncomingDocumentType, ActorRole, IncomingMarketMessageStream>
        {
            { DocumentFormat.Json, IncomingDocumentType.RequestAggregatedMeasureData, ActorRole.BalanceResponsibleParty, ReadFile(@"IncomingMessages\RequestAggregatedMeasureDataAsDdk.json") },
            { DocumentFormat.Json, IncomingDocumentType.RequestWholesaleSettlement, ActorRole.EnergySupplier, ReadFile(@"IncomingMessages\RequestWholesaleSettlement.json") },
            //{ DocumentFormat.Ebix, IncomingDocumentType.NotifyValidatedMeasureData, ActorRole.GridAccessProvider, ReadFile(@"IncomingMessages\EbixMeteredDataForMeteringPoint.xml") },
            { DocumentFormat.Xml, IncomingDocumentType.NotifyValidatedMeasureData, ActorRole.GridAccessProvider, ReadFile(@"IncomingMessages\MeteredDataForMeteringPoint.xml") },
            { DocumentFormat.Json, IncomingDocumentType.NotifyValidatedMeasureData, ActorRole.GridAccessProvider, ReadFile(@"IncomingMessages\MeteredDataForMeteringPoint.json") },
        };

        return data;
    }

    public static IEnumerable<object[]> InvalidIncomingRequestMessages()
    {
        return
        [
            [
                DocumentFormat.Json,
                IncomingDocumentType.RequestAggregatedMeasureData,
                ReadFile(@"IncomingMessages\FailSchemeValidationAggregatedMeasureData.json"),
            ],
            [
                DocumentFormat.Json,
                IncomingDocumentType.RequestWholesaleSettlement,
                ReadFile(@"IncomingMessages\FailSchemeValidationRequestWholesaleSettlement.json"),
            ],
            [
                DocumentFormat.Json,
                IncomingDocumentType.RequestWholesaleSettlement,
                ReadFile(
                    @"IncomingMessages\RequestWholesaleSettlementWithUnusedBusinessReason.json"),
            ],
        ];
    }

    [Theory]
    [MemberData(nameof(ValidIncomingRequestMessages))]
    public async Task When_MessageIsReceived_Then_BodyAndTransactionAndMessageIdArePresentOnTheInternalRepresentation(
        DocumentFormat format,
        IncomingDocumentType incomingDocumentType,
        ActorRole actorRole,
        IncomingMarketMessageStream incomingMarketMessageStream)
    {
        // Assert
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(
                senderActorNumber,
                Restriction.Owned,
                actorRole,
                ActorId));

        // Act
        var registerAndSendAsync = await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            incomingMarketMessageStream,
            format,
            incomingDocumentType,
            format,
            CancellationToken.None);

        // Assert
        registerAndSendAsync.IsErrorResponse.Should().BeFalse(registerAndSendAsync.MessageBody);

        var transactionIds = await GetTransactionIdsAsync(senderActorNumber);
        var messageIds = await GetMessageIdsAsync(senderActorNumber);
        var message = _senderSpy.LatestMessage;

        Assert.Multiple(
            () => Assert.NotNull(message),
            () => Assert.Single(transactionIds),
            () => Assert.Single(messageIds));
    }

    [Fact]
    public async Task AndGiven_DdmMdrHackIsApplicable_When_MessageIsReceived_Then_BodyAndTransactionAndMessageIdArePresentOnTheInternalRepresentation()
    {
        // Assert
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.GridAccessProvider, ActorId));

        // Act
        await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            ReadFile(@"IncomingMessages\RequestAggregatedMeasureDataAsMdr.json"),
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            DocumentFormat.Json,
            CancellationToken.None);

        // Assert
        var transactionIds = await GetTransactionIdsAsync(senderActorNumber);
        var messageIds = await GetMessageIdsAsync(senderActorNumber);
        var message = _senderSpy.LatestMessage;

        using var assertionScope = new AssertionScope();
        message.Should().NotBeNull();
        transactionIds.Should().ContainSingle();
        messageIds.Should().ContainSingle();
    }

    [Theory]
    [MemberData(nameof(ValidIncomingRequestMessages))]
    public async Task AndGiven_ServiceBusFails_When_MessageIsReceived_Then_TransactionAndMessageIdsAreNotSaved(
        DocumentFormat format,
        IncomingDocumentType incomingDocumentType,
        ActorRole actorRole,
        IncomingMarketMessageStream incomingMarketMessageStream)
    {
        // Assert
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(
                senderActorNumber,
                Restriction.Owned,
                actorRole,
                ActorId));

        _senderSpy.ShouldFail = true;

        // Act & Assert
        await Assert.ThrowsAsync<ServiceBusException>(
            () => _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
                incomingMarketMessageStream,
                format,
                incomingDocumentType,
                format,
                CancellationToken.None));

        var transactionIds = await GetTransactionIdsAsync(senderActorNumber);
        var messageIds = await GetMessageIdsAsync(senderActorNumber);
        var message = _senderSpy.LatestMessage;

        Assert.Multiple(
            () => Assert.Null(message),
            () => Assert.Empty(transactionIds),
            () => Assert.Empty(messageIds));

        _senderSpy.ShouldFail = false;
    }

    [Theory]
    [MemberData(nameof(ValidIncomingRequestMessages))]
    public async Task AndGiven_MultipleRequestsWithSameTransactionAndMessageId_When_MessageIsReceived_Then_OnlyOneRequestPrTransactionIdAndMessageIdIsAccepted(
        DocumentFormat format,
        IncomingDocumentType incomingDocumentType,
        ActorRole actorRole,
        IncomingMarketMessageStream incomingMarketMessageStream)
    {
        // Arrange
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(
                senderActorNumber,
                Restriction.Owned,
                actorRole,
                ActorId));

        // new scope to simulate a race condition.
        var sessionProvider = GetService<IServiceProvider>();
        using var secondScope = sessionProvider.CreateScope();
        var authenticatedActorInSecondScope = secondScope.ServiceProvider.GetService<AuthenticatedActor>();
        var secondParser = secondScope.ServiceProvider.GetRequiredService<IIncomingMessageClient>();

        authenticatedActorInSecondScope!.SetAuthenticatedActor(
            new ActorIdentity(senderActorNumber, Restriction.None, ActorRole.BalanceResponsibleParty, Guid.Parse("00000000-0000-0000-0000-000000000002")));

        var task01 = _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            incomingMarketMessageStream,
            format,
            incomingDocumentType,
            format,
            CancellationToken.None);

        var task02 = secondParser.ReceiveIncomingMarketMessageAsync(
            incomingMarketMessageStream,
            format,
            incomingDocumentType,
            format,
            CancellationToken.None);

        // Act
        IEnumerable<ResponseMessage> results = await Task.WhenAll(task01, task02);

        // Assert
        var transactionIds = await GetTransactionIdsAsync(senderActorNumber);
        var messageIds = await GetMessageIdsAsync(senderActorNumber);
        var message = _senderSpy.LatestMessage;

        Assert.Multiple(
            () => Assert.NotNull(results),
            () => Assert.NotNull(message),
            () => Assert.Single(transactionIds),
            () => Assert.Single(messageIds));
    }

    [Theory]
    [MemberData(nameof(ValidIncomingRequestMessages))]
    public async Task AndGiven_ASecondRequestWithSameTransactionIdAndMessageId_When_MessageIsReceived_Then_ItIsRejected(
        DocumentFormat format,
        IncomingDocumentType incomingDocumentType,
        ActorRole actorRole,
        IncomingMarketMessageStream incomingMarketMessageStream)
    {
        // Arrange
        var exceptedDuplicateTransactionIdDetectedErrorCode = format == DocumentFormat.Ebix ? "B2B-009" : "00102";
        var exceptedDuplicateMessageIdDetectedErrorCode = format == DocumentFormat.Ebix ? "B2B-003" : "00101";

        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        var authenticatedActorRole = actorRole;

        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(
                senderActorNumber,
                Restriction.Owned,
                authenticatedActorRole,
                ActorId));

        // new scope to simulate a race condition.
        var sessionProvider = GetService<IServiceProvider>();
        using var secondScope = sessionProvider.CreateScope();
        var authenticatedActorInSecondScope = secondScope.ServiceProvider.GetService<AuthenticatedActor>();
        var secondParser = secondScope.ServiceProvider.GetRequiredService<IIncomingMessageClient>();

        authenticatedActorInSecondScope!.SetAuthenticatedActor(
            new ActorIdentity(
                senderActorNumber,
                Restriction.None,
                authenticatedActorRole,
                ActorId));

        var task01 = _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            incomingMarketMessageStream,
            format,
            incomingDocumentType,
            format,
            CancellationToken.None);

        var task02 = secondParser.ReceiveIncomingMarketMessageAsync(
            incomingMarketMessageStream,
            format,
            incomingDocumentType,
            format,
            CancellationToken.None);

        // Act
        IEnumerable<ResponseMessage> results = await Task.WhenAll(task01, task02);

        // Assert
        using var assertionScope = new AssertionScope();
        results.Should().NotBeNullOrEmpty();
        results.Should()
            .ContainSingle(result => result.IsErrorResponse, "we expect the results contains exactly 1 error");

        var errorResult = results.Single(result => result.IsErrorResponse);
        errorResult.MessageBody.Should()
            .ContainAny(
                exceptedDuplicateTransactionIdDetectedErrorCode,
                exceptedDuplicateMessageIdDetectedErrorCode);
    }

    [Theory]
    [MemberData(nameof(InvalidIncomingRequestMessages))]
    public async Task AndGiven_FaultyRequest_When_MessageIsReceived_Then_TransactionAndMessageIdsAreNotSaved(
        DocumentFormat format,
        IncomingDocumentType incomingDocumentType,
        IncomingMarketMessageStream incomingMarketMessageStream)
    {
        // Assert
        var senderActorNumber = ActorNumber.Create("5799999933318");
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty, ActorId));

        // Act
        await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            incomingMarketMessageStream,
            format,
            incomingDocumentType,
            format,
            CancellationToken.None);

        // Assert
        var transactionIds = await GetTransactionIdsAsync(senderActorNumber);
        var messageIds = await GetMessageIdsAsync(senderActorNumber);
        var message = _senderSpy.LatestMessage;

        Assert.Multiple(
            () => Assert.Null(message),
            () => Assert.Empty(transactionIds),
            () => Assert.Empty(messageIds));
    }

    [Fact]
    public async Task When_MessageIsReceived_Then_IncomingMessageIsArchivedWithCorrectContent()
    {
        // Assert
        const string messageIdFromFile = "123564789123564789123564789123564789";

        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty, ActorId));

        var messageStream = ReadFile(@"IncomingMessages\RequestAggregatedMeasureDataAsDdk.json");

        // Act
        await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            messageStream,
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            DocumentFormat.Json,
            CancellationToken.None);

        // Assert
        using var assertionScope = new AssertionScope();
        var incomingMessageContent = await GetStreamContentAsStringAsync(messageStream.Stream);
        var archivedMessageFileStorageReference =
            await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageIdFromFile);

        archivedMessageFileStorageReference.Should().NotBeNull();

        var archivedMessageFileContent = await GetFileContentFromFileStorageAsync(
            "archived",
            archivedMessageFileStorageReference!);

        archivedMessageFileContent.Should().Be(incomingMessageContent);
    }

    [Fact]
    public async Task When_MeteredDataForMeteringPointMessageIsReceived_Then_IncomingMessageIsNotArchived()
    {
        // Assert
        const string messageIdFromFile = "111131835";

        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5790001330552");
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.MeteredDataResponsible, ActorId));

        var messageStream = ReadFile(@"IncomingMessages\EbixMeteredDataForMeteringPoint.xml");

        // Act
        await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            messageStream,
            DocumentFormat.Ebix,
            IncomingDocumentType.NotifyValidatedMeasureData,
            DocumentFormat.Ebix,
            CancellationToken.None);

        // Assert
        var archivedMessage = await GetArchivedMessageFromDatabaseAsync(messageIdFromFile);

        archivedMessage?.Should().BeNull();
    }

    [Theory]
    [InlineData(@"IncomingMessages\RequestAggregatedMeasureDataAsDdk.json", "RequestAggregatedMeasureData")]
    [InlineData(@"IncomingMessages\RequestWholesaleSettlement.json", "RequestWholesaleSettlement")]
    public async Task When_MessageIsReceived_Then_IncomingMessageIsArchivedWithCorrectData(string path, string incomingDocumentTypeName)
    {
        // Arrange
        const int year = 2024;
        const int month = 01;
        const int date = 05;
        const int hour = 04;
        const int minute = 23;

        const string senderActorRole = "DDK";
        const string messageIdFromFile = "123564789123564789123564789123564789";
        const string businessReasonFromFile = "D05";
        const string receiverActorNumberFromFile = "5790001330552";
        const string receiverActorRoleFromFile = "DGL";

        var incomingDocumentType = IncomingDocumentType.FromName(incomingDocumentTypeName)!;

        var expectedTimestamp = new DateTime(year, month, date, hour, minute, 0, DateTimeKind.Utc);
        _clockStub.SetCurrentInstant(expectedTimestamp.ToInstant());

        var senderActorNumber = ActorNumber.Create("5799999933318");
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty, ActorId));

        var messageStream = ReadFile(path);

        // Act
        await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            messageStream,
            DocumentFormat.Json,
            incomingDocumentType,
            DocumentFormat.Json,
            CancellationToken.None);

        // Assert
        var archivedMessage = await GetArchivedMessageFromDatabaseAsync(messageIdFromFile);
        ((object?)archivedMessage).Should().NotBeNull("because an archived message should exists");

        var expectedFileStorageReference =
            $"{senderActorNumber.Value}/{year:0000}/{month:00}/{date:00}/{archivedMessage!.Id:N}";

        var assertProperties = new Dictionary<string, Action<object?>>
        {
            { "BusinessReason", businessReason => businessReason.Should().Be(businessReasonFromFile) },
            { "CreatedAt", createdAt => createdAt.Should().Be(expectedTimestamp) },
            { "DocumentType", documentType => documentType.Should().Be(incomingDocumentType.Name) },
            { "EventIds", eventIds => eventIds.Should().BeNull() },
            {
                "FileStorageReference",
                fileStorageReference => fileStorageReference.Should().Be(expectedFileStorageReference)
            },
            { "Id", id => id.Should().NotBeNull() },
            { "MessageId", messageId => messageId.Should().Be(messageIdFromFile) },
            { "ReceiverNumber", receiverNumber => receiverNumber.Should().Be(receiverActorNumberFromFile) },
            { "ReceiverRoleCode", receiverRoleCode => receiverRoleCode.Should().Be(receiverActorRoleFromFile) },
            { "SenderRoleCode", senderRoleCode => senderRoleCode.Should().Be(senderActorRole) },
            { "RecordId", recordId => recordId.Should().NotBeNull() },
            { "RelatedToMessageId", relatedToMessageId => relatedToMessageId.Should().BeNull() },
            { "SenderNumber", senderNumber => senderNumber.Should().Be(senderActorNumber.Value) },
        };

        using var assertionScope = new AssertionScope();
        var archivedMessageAsDictionary = (IDictionary<string, object>)archivedMessage;

        foreach (var assertProperty in assertProperties)
        {
            assertProperty.Value(archivedMessageAsDictionary[assertProperty.Key]);
        }

        assertProperties.Should()
            .HaveSameCount(archivedMessageAsDictionary, "because all archived message properties should be asserted");

        foreach (var dbPropertyName in archivedMessageAsDictionary.Keys)
        {
            assertProperties.Keys.Should().Contain(dbPropertyName);
        }
    }

    protected override void Dispose(bool disposing)
    {
        _incomingMessageContext.Dispose();
        base.Dispose(disposing);
    }

    private static IncomingMarketMessageStream ReadFile(string path)
    {
        var jsonDoc = File.ReadAllText(path);

        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;

        return new IncomingMarketMessageStream(stream);
    }

    private async Task<List<dynamic>> GetTransactionIdsAsync(ActorNumber senderNumber)
    {
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT [TransactionId] FROM [dbo].[TransactionRegistry] WHERE SenderId = '{senderNumber.Value}'";
        var results = await connection.QueryAsync(sql);
        return results.ToList();
    }

    private async Task<List<dynamic>> GetMessageIdsAsync(ActorNumber senderNumber)
    {
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT [MessageId] FROM [dbo].[MessageRegistry] WHERE SenderId = '{senderNumber.Value}'";
        var results = await connection.QueryAsync<dynamic>(sql);
        return results.ToList();
    }
}
