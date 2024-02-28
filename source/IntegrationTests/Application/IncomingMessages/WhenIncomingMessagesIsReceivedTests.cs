﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;

public class WhenIncomingMessagesIsReceivedTests : TestBase
{
    private readonly IIncomingMessageClient _incomingMessagesRequest;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly IncomingMessagesContext _incomingMessageContext;
    private readonly SystemDateTimeProviderStub _dateTimeProvider;

    public WhenIncomingMessagesIsReceivedTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
        _incomingMessagesRequest = GetService<IIncomingMessageClient>();
        _incomingMessageContext = GetService<IncomingMessagesContext>();
        _dateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
    }

    [Fact]
    public async Task Incoming_message_is_received()
    {
      // Assert
      var authenticatedActor = GetService<AuthenticatedActor>();
      var senderActorNumber = ActorNumber.Create("5799999933318");
      authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty));

      // Act
      await _incomingMessagesRequest.RegisterAndSendAsync(
          ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
          DocumentFormat.Json,
          IncomingDocumentType.RequestAggregatedMeasureData,
          CancellationToken.None);

      // Assert
      var transactionIds = await GetTransactionIdsAsync(senderActorNumber);
      var messageIds = await GetMessageIdsAsync(senderActorNumber);
      var message = _senderSpy.Message;

      Assert.Multiple(
          () => Assert.NotNull(message),
          () => Assert.Single(transactionIds),
          () => Assert.Single(messageIds));
    }

    [Fact]
    public async Task Transaction_and_message_ids_are_not_saved_when_failing_to_send_to_ServiceBus()
    {
        // Assert
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty));
        _senderSpy.ShouldFail = true;

        // Act & Assert
        await Assert.ThrowsAsync<ServiceBusException>(() => _incomingMessagesRequest.RegisterAndSendAsync(
            ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            CancellationToken.None));

        var transactionIds = await GetTransactionIdsAsync(senderActorNumber);
        var messageIds = await GetMessageIdsAsync(senderActorNumber);
        var message = _senderSpy.Message;

        Assert.Multiple(
            () => Assert.Null(message),
            () => Assert.Empty(transactionIds),
            () => Assert.Empty(messageIds));

        _senderSpy.ShouldFail = false;
    }

    [Fact]
    public async Task Only_one_request_pr_transactionId_and_messageId_is_accepted()
    {
        // Arrange
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty));

        // new scope to simulate a race condition.
        var sessionProvider = GetService<IServiceProvider>();
        using var secondScope = sessionProvider.CreateScope();
        var authenticatedActorInSecondScope = secondScope.ServiceProvider.GetService<AuthenticatedActor>();
        var secondParser = secondScope.ServiceProvider.GetRequiredService<IIncomingMessageClient>();

        authenticatedActorInSecondScope!.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, restriction: Restriction.None, ActorRole.BalanceResponsibleParty));

        var task01 = _incomingMessagesRequest.RegisterAndSendAsync(
            ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            CancellationToken.None);
        var task02 = secondParser.RegisterAndSendAsync(
            ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            CancellationToken.None);

        // Act
        IEnumerable<ResponseMessage> results = await Task.WhenAll(task01, task02);

        // Assert
        var transactionIds = await GetTransactionIdsAsync(senderActorNumber);
        var messageIds = await GetMessageIdsAsync(senderActorNumber);
        var message = _senderSpy.Message;

        Assert.Multiple(
            () => Assert.NotNull(results),
            () => Assert.NotNull(message),
            () => Assert.Single(transactionIds),
            () => Assert.Single(messageIds));
    }

    [Fact]
    public async Task Second_request_with_same_transactionId_and_messageId_is_rejected()
    {
        // Arrange
        var exceptedDuplicateTransactionIdDetectedErrorCode = "00110";
        var exceptedDuplicateMessageIdDetectedErrorCode = "00101";
        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty));

        // new scope to simulate a race condition.
        var sessionProvider = GetService<IServiceProvider>();
        using var secondScope = sessionProvider.CreateScope();
        var authenticatedActorInSecondScope = secondScope.ServiceProvider.GetService<AuthenticatedActor>();
        var secondParser = secondScope.ServiceProvider.GetRequiredService<IIncomingMessageClient>();

        authenticatedActorInSecondScope!.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, restriction: Restriction.None, ActorRole.BalanceResponsibleParty));

        var task01 = _incomingMessagesRequest.RegisterAndSendAsync(
            ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            CancellationToken.None);
        var task02 = secondParser.RegisterAndSendAsync(
            ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json"),
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            CancellationToken.None);

        // Act
        IEnumerable<ResponseMessage> results = await Task.WhenAll(task01, task02);

        // Assert
        Assert.Multiple(
            () => Assert.NotNull(results),
            () => Assert.Single(results.Where(result => result.IsErrorResponse)),
            () => Assert.Single(results.Where(result =>
                result.MessageBody.Contains(exceptedDuplicateTransactionIdDetectedErrorCode, StringComparison.OrdinalIgnoreCase)
                || result.MessageBody.Contains(exceptedDuplicateMessageIdDetectedErrorCode, StringComparison.OrdinalIgnoreCase))));
    }

    [Fact]
    public async Task Transaction_and_message_ids_are_not_saved_when_receiving_a_faulted_request()
    {
        // Assert
        var senderActorNumber = ActorNumber.Create("5799999933318");
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty));

        // Act
        await _incomingMessagesRequest.RegisterAndSendAsync(
            ReadJsonFile("Application\\IncomingMessages\\FailSchemeValidationAggregatedMeasureData.json"),
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            CancellationToken.None);

        // Assert
        var transactionIds = await GetTransactionIdsAsync(senderActorNumber);
        var messageIds = await GetMessageIdsAsync(senderActorNumber);
        var message = _senderSpy.Message;

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
        var messageStream = ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json");
        var messageIdFromFile = "123564789123564789123564789123564789";
        // Act
        await _incomingMessagesRequest.RegisterAndSendAsync(
            messageStream,
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            CancellationToken.None);

        // Assert
        var incomingMessageContent = await GetStreamContentAsStringAsync(messageStream.Stream);
        var archivedMessageFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageIdFromFile);
        var archivedMessageFileContent = await GetFileContentFromFileStorageAsync("archived", archivedMessageFileStorageReference);
        archivedMessageFileContent.Should().Be(incomingMessageContent);
    }

    [Fact]
    public async Task Incoming_message_is_archived_with_correct_file_storage_reference()
    {
        // Assert
        int year = 2024,
            month = 01,
            date = 05;
        _dateTimeProvider.SetNow(Instant.FromUtc(year, month, date, 04, 23));

        var authenticatedActor = GetService<AuthenticatedActor>();
        var senderActorNumber = ActorNumber.Create("5799999933318");
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(senderActorNumber, Restriction.Owned, ActorRole.BalanceResponsibleParty));
        var messageStream = ReadJsonFile("Application\\IncomingMessages\\RequestAggregatedMeasureData.json");
        var messageIdFromFile = "123564789123564789123564789123564789";
        // Act
        await _incomingMessagesRequest.RegisterAndSendAsync(
            messageStream,
            DocumentFormat.Json,
            IncomingDocumentType.RequestAggregatedMeasureData,
            CancellationToken.None);

        // Assert
        var archivedMessageId = await GetArchivedMessageIdFromDatabaseAsync(messageIdFromFile);
        var archivedMessageFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageIdFromFile);
        archivedMessageFileStorageReference.Should().Be($"{senderActorNumber.Value}/{year:0000}/{month:00}/{date:00}/{archivedMessageId:N}");
    }

    protected override void Dispose(bool disposing)
    {
        _senderSpy.Dispose();
        _serviceBusClientSenderFactory.Dispose();
        _incomingMessageContext.Dispose();
        base.Dispose(disposing);
    }

    private static IncomingMessageStream ReadJsonFile(string path)
    {
        var jsonDoc = File.ReadAllText(path);

        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;

        return new IncomingMessageStream(stream);
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
