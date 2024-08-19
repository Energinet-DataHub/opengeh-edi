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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;

public class WhenIncomingMessagesIsReceivedTests : TestBase
{
    private readonly IIncomingMessageClient _incomingMessagesRequest;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly IncomingMessagesContext _incomingMessageContext;
    private readonly SystemDateTimeProviderStub _dateTimeProvider;

    public WhenIncomingMessagesIsReceivedTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
        _incomingMessagesRequest = GetService<IIncomingMessageClient>();
        _incomingMessageContext = GetService<IncomingMessagesContext>();
        _dateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
    }

    public static IEnumerable<object[]> ValidIncomingRequestMessages()
    {
        return new List<object[]>
        {
            new object[] { DocumentFormat.Json, IncomingDocumentType.RequestAggregatedMeasureData, ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureDataAsDdk.json") },
            new object[] { DocumentFormat.Json, IncomingDocumentType.RequestWholesaleSettlement, ReadJsonFile("Application\\IncomingMessages\\RequestWholesaleSettlement.json") },
        };
    }

    public static IEnumerable<object[]> InvalidIncomingRequestMessages()
    {
        return new List<object[]>
        {
            new object[] { DocumentFormat.Json, IncomingDocumentType.RequestAggregatedMeasureData, ReadJsonFile("Application\\IncomingMessages\\FailSchemeValidationAggregatedMeasureData.json") },
            new object[] { DocumentFormat.Json, IncomingDocumentType.RequestWholesaleSettlement, ReadJsonFile("Application\\IncomingMessages\\FailSchemeValidationRequestWholesaleSettlement.json") },
            new object[] { DocumentFormat.Json, IncomingDocumentType.RequestWholesaleSettlement, ReadJsonFile("Application\\IncomingMessages\\RequestWholesaleSettlementWithUnusedBusinessReason.json") },
        };
    }

    [Theory]
    [MemberData(nameof(ValidIncomingRequestMessages))]
    public async Task Incoming_message_is_received(DocumentFormat format, IncomingDocumentType incomingDocumentType, IncomingMarketMessageStream incomingMarketMessageStream)
    {
      // Assert
      var authenticatedActor = GetService<AuthenticatedActor>();
      var senderActorNumber = ActorNumber.Create("5799999933318");
      authenticatedActor.SetAuthenticatedActor(
          new ActorIdentity(
              senderActorNumber,
              Restriction.Owned,
              incomingDocumentType == IncomingDocumentType.RequestAggregatedMeasureData ? ActorRole.BalanceResponsibleParty : ActorRole.EnergySupplier));

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
    public async Task Incoming_message_is_received_with_Ddm_Mdr_hack()
    {
        // Assert
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.GridOperator));

        // Act
        await _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
            ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureDataAsMdr.json"),
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
    public async Task Transaction_and_message_ids_are_not_saved_when_failing_to_send_to_ServiceBus(DocumentFormat format, IncomingDocumentType incomingDocumentType, IncomingMarketMessageStream incomingMarketMessageStream)
    {
        // Assert
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(
                senderActorNumber,
                Restriction.Owned,
                incomingDocumentType == IncomingDocumentType.RequestAggregatedMeasureData ? ActorRole.BalanceResponsibleParty : ActorRole.EnergySupplier));

        _senderSpy.ShouldFail = true;

        // Act & Assert
        await Assert.ThrowsAsync<ServiceBusException>(() => _incomingMessagesRequest.ReceiveIncomingMarketMessageAsync(
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
    public async Task Only_one_request_pr_transactionId_and_messageId_is_accepted(DocumentFormat format, IncomingDocumentType incomingDocumentType, IncomingMarketMessageStream incomingMarketMessageStream)
    {
        // Arrange
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(
                senderActorNumber,
                Restriction.Owned,
                incomingDocumentType == IncomingDocumentType.RequestAggregatedMeasureData ? ActorRole.BalanceResponsibleParty : ActorRole.EnergySupplier));

        // new scope to simulate a race condition.
        var sessionProvider = GetService<IServiceProvider>();
        using var secondScope = sessionProvider.CreateScope();
        var authenticatedActorInSecondScope = secondScope.ServiceProvider.GetService<AuthenticatedActor>();
        var secondParser = secondScope.ServiceProvider.GetRequiredService<IIncomingMessageClient>();

        authenticatedActorInSecondScope!.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, restriction: Restriction.None, ActorRole.BalanceResponsibleParty));

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
    public async Task Second_request_with_same_transactionId_and_messageId_is_rejected(DocumentFormat format, IncomingDocumentType incomingDocumentType, IncomingMarketMessageStream incomingMarketMessageStream)
    {
        // Arrange
        var exceptedDuplicateTransactionIdDetectedErrorCode = "00110";
        var exceptedDuplicateMessageIdDetectedErrorCode = "00101";
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        var authenticatedActorRole = incomingDocumentType == IncomingDocumentType.RequestAggregatedMeasureData ? ActorRole.BalanceResponsibleParty : ActorRole.EnergySupplier;
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(
                senderActorNumber,
                Restriction.Owned,
                authenticatedActorRole));

        // new scope to simulate a race condition.
        var sessionProvider = GetService<IServiceProvider>();
        using var secondScope = sessionProvider.CreateScope();
        var authenticatedActorInSecondScope = secondScope.ServiceProvider.GetService<AuthenticatedActor>();
        var secondParser = secondScope.ServiceProvider.GetRequiredService<IIncomingMessageClient>();

        authenticatedActorInSecondScope!.SetAuthenticatedActor(new ActorIdentity(
            senderActorNumber,
            restriction: Restriction.None,
            authenticatedActorRole));

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
        results.Should().ContainSingle(result => result.IsErrorResponse, because: "we expect the results contains exactly 1 error");

        var errorResult = results.Single(result => result.IsErrorResponse);
        errorResult.MessageBody.Should().ContainAny([
            exceptedDuplicateTransactionIdDetectedErrorCode,
            exceptedDuplicateMessageIdDetectedErrorCode]);
    }

    [Theory]
    [MemberData(nameof(InvalidIncomingRequestMessages))]
    public async Task Transaction_and_message_ids_are_not_saved_when_receiving_a_faulted_request(DocumentFormat format, IncomingDocumentType incomingDocumentType, IncomingMarketMessageStream incomingMarketMessageStream)
    {
        // Assert
        var senderActorNumber = ActorNumber.Create("5799999933318");
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty));

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
    public async Task Incoming_message_is_archived_with_correct_content()
    {
        // Assert
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty));
        var messageStream = ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureDataAsDdk.json");
        var messageIdFromFile = "123564789123564789123564789123564789";
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
        var archivedMessageFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageIdFromFile);
        archivedMessageFileStorageReference.Should().NotBeNull();

        var archivedMessageFileContent = await GetFileContentFromFileStorageAsync(
            "archived",
            archivedMessageFileStorageReference!);

        archivedMessageFileContent.Should().Be(incomingMessageContent);
    }

    [Theory]
    [InlineData("Application\\IncomingMessages\\RequestAggregatedMeasureDataAsDdk.json", "RequestAggregatedMeasureData")]
    [InlineData("Application\\IncomingMessages\\RequestWholesaleSettlement.json", "RequestWholesaleSettlement")]
    public async Task Incoming_message_is_archived_with_correct_data(string path, string incomingDocumentTypeName)
    {
        // Arrange
        var incomingDocumentType = IncomingDocumentType.FromName(incomingDocumentTypeName)!;
        int year = 2024,
            month = 01,
            date = 05,
            hour = 04,
            minute = 23;
        var expectedTimestamp = new DateTime(year, month, date, hour, minute, 0, DateTimeKind.Utc);
        _dateTimeProvider.SetNow(expectedTimestamp.ToInstant());

        var senderActorNumber = ActorNumber.Create("5799999933318");
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty));
        var messageStream = ReadJsonFile(path);
        var messageIdFromFile = "123564789123564789123564789123564789";
        var businessReasonFromFile = "D05";
        var receiverActorNumberFromFile = "5790001330552";

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

        var expectedFileStorageReference = $"{senderActorNumber.Value}/{year:0000}/{month:00}/{date:00}/{archivedMessage!.Id:N}";
        var assertProperties = new Dictionary<string, Action<object?>>
        {
            { "BusinessReason", businessReason => businessReason.Should().Be(businessReasonFromFile) },
            { "CreatedAt", createdAt => createdAt.Should().Be(expectedTimestamp) },
            { "DocumentType", documentType => documentType.Should().Be(incomingDocumentType.Name) },
            { "EventIds", eventIds => eventIds.Should().BeNull() },
            { "FileStorageReference", fileStorageReference => fileStorageReference.Should().Be(expectedFileStorageReference) },
            { "Id", id => id.Should().NotBeNull() },
            { "MessageId", messageId => messageId.Should().Be(messageIdFromFile) },
            { "ReceiverNumber", receiverNumber => receiverNumber.Should().Be(receiverActorNumberFromFile) },
            { "RecordId", recordId => recordId.Should().NotBeNull() },
            { "RelatedToMessageId", relatedToMessageId => relatedToMessageId.Should().BeNull() },
            { "SenderNumber", senderNumber => senderNumber.Should().Be(senderActorNumber.Value) },
        };

        using var assertionScope = new AssertionScope();
        var archivedMessageAsDictionary = (IDictionary<string, object>)archivedMessage;

        foreach (var assertProperty in assertProperties)
            assertProperty.Value(archivedMessageAsDictionary[assertProperty.Key]);

        assertProperties.Should().HaveSameCount(archivedMessageAsDictionary, "because all archived message properties should be asserted");

        foreach (var dbPropertyName in archivedMessageAsDictionary.Keys)
            assertProperties.Keys.Should().Contain(dbPropertyName);
    }

    protected override void Dispose(bool disposing)
    {
        _senderSpy.Dispose();
        _serviceBusClientSenderFactory.Dispose();
        _incomingMessageContext.Dispose();
        base.Dispose(disposing);
    }

    private static IncomingMarketMessageStream ReadJsonFile(string path)
    {
        var jsonDoc = File.ReadAllText(path);

        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;

        return new IncomingMarketMessageStream(stream);
    }

    private async Task<List<dynamic>> GetTransactionIdsAsync(ActorNumber senderNumber)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT [TransactionId] FROM [dbo].[TransactionRegistry] WHERE SenderId = '{senderNumber.Value}'";
        var results = await connection.QueryAsync(sql);
        return results.ToList();
    }

    private async Task<List<dynamic>> GetMessageIdsAsync(ActorNumber senderNumber)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT [MessageId] FROM [dbo].[MessageRegistry] WHERE SenderId = '{senderNumber.Value}'";
        var results = await connection.QueryAsync<dynamic>(sql);
        return results.ToList();
    }
}
