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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenAPeekIsRequestedTests : TestBase
{
    private readonly EnergyResultMessageDtoBuilder _energyResultMessageDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly SystemDateTimeProviderStub _dateTimeProvider;

    public WhenAPeekIsRequestedTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _energyResultMessageDtoBuilder = new EnergyResultMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        _dateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
    }

    public static object[][] GetUnusedDataHubTypesWithDocumentFormat()
    {
        var documentFormats = EnumerationType.GetAll<DocumentFormat>();
        var dataHubTypesWithUnused = Assembly.GetAssembly(typeof(DataHubTypeWithUnused<>))!
            .GetTypes()
            .Where(
                t => t.BaseType is { IsGenericType: true }
                     && t.BaseType.GetGenericTypeDefinition() == typeof(DataHubTypeWithUnused<>))
            .ToArray();

        var typesWithDocumentFormats = dataHubTypesWithUnused
            .SelectMany(t => documentFormats.Select(df => new object[] { t, df }))
            .ToArray();

        return typesWithDocumentFormats;
    }

    [Fact]
    public async Task When_no_messages_are_available_return_empty_result()
    {
        var message = _energyResultMessageDtoBuilder.Build();
        await EnqueueMessage(message);

        var result = await PeekMessageAsync(MessageCategory.None);

        Assert.Null(result.Bundle);
        Assert.True(await BundleIsRegistered());
    }

    [Fact]
    public async Task A_message_bundle_is_returned()
    {
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var result = await PeekMessageAsync(MessageCategory.Aggregations);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.NotifyAggregatedMeasureData)
            .IsBusinessReason(BusinessReason.BalanceFixing)
            .HasSerieRecordCount(1);
    }

    [Fact]
    public async Task Ensure_same_bundle_is_returned_if_not_dequeued()
    {
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var firstPeekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        ClearDbContextCaches(); // Else the MarketDocument is cached in Entity Framework
        var secondPeekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        Assert.NotNull(firstPeekResult.MessageId);
        Assert.NotNull(secondPeekResult.MessageId);

        var firstPeekContent = await GetStreamContentAsStringAsync(firstPeekResult.Bundle!);
        var secondPeekContent = await GetStreamContentAsStringAsync(secondPeekResult.Bundle!);
        Assert.Equal(firstPeekContent, secondPeekContent);
    }

    [Fact]
    public async Task A_market_document_is_archived_with_correct_content()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        // Act
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations, null, null, DocumentFormat.Ebix);

        // Assert
        using var assertScope = new AssertionScope();
        peekResult.Bundle.Should().NotBeNull();

        var peekResultFileContent = await GetStreamContentAsStringAsync(peekResult.Bundle!);
        var archivedMessageFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(peekResult.MessageId!);
        archivedMessageFileStorageReference.Should().NotBeNull();

        var archivedMessageFileContent = await GetFileContentFromFileStorageAsync(
            "archived",
            archivedMessageFileStorageReference!);

        archivedMessageFileContent.Should().Be(peekResultFileContent);
    }

    [Fact]
    public async Task A_market_document_is_archived_with_correct_file_storage_reference()
    {
        // Arrange
        int year = 2024,
            month = 01,
            date = 02;
        _dateTimeProvider.SetNow(Instant.FromUtc(year, month, date, 11, 07));
        var receiverNumber = SampleData.NewEnergySupplierNumber;
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(receiverNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        // Act
        var result = await PeekMessageAsync(MessageCategory.Aggregations);

        // Assert
        result.Bundle.Should().NotBeNull();

        var archivedMessageId = await GetArchivedMessageIdFromDatabaseAsync(result.MessageId!);
        var fileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(result.MessageId!);
        fileStorageReference.Should().Be($"{receiverNumber}/{year:0000}/{month:00}/{date:00}/{archivedMessageId:N}");
    }

    [Fact]
    public async Task A_market_document_is_added_to_database()
    {
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var result = await PeekMessageAsync(MessageCategory.Aggregations);

        result.MessageId.Should().NotBeNull();

        var marketDocumentExists = await MarketDocumentExists(result.MessageId!);
        marketDocumentExists.Should().BeTrue();
    }

    [Fact]
    public async Task The_created_market_document_uses_the_archived_message_file_reference()
    {
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        var marketDocumentFileStorageReference = await GetMarketDocumentFileStorageReferenceFromDatabaseAsync(peekResult.MessageId!);
        var archivedMessageFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(peekResult.MessageId!);

        marketDocumentFileStorageReference.Should().Be(archivedMessageFileStorageReference);
    }

    /// <summary>
    /// This test verifies the "hack" for a MDR/GridOperator actor which is the same Actor but with two distinct roles MDR and GridOperator
    /// The actor uses the MDR (MeteredDataResponsible) role when making request (RequestAggregatedMeasureData)
    /// but uses the DDM (GridOperator) role when peeking.
    /// This means that when peeking as a MDR we should peek the DDM queue
    /// </summary>
    [Fact]
    public async Task When_PeekingAsMeteredDataResponsible_Then_FindsGridOperatorMessages()
    {
        // Arrange
        var actorNumber = ActorNumber.Create("1234567890123");
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(actorNumber.Value)
            .WithReceiverRole(ActorRole.GridOperator)
            .Build();
        await EnqueueMessage(message);

        // Act
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations, actorNumber: actorNumber, actorRole: ActorRole.MeteredDataResponsible);

        // Assert
        peekResult.MessageId.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(GetUnusedDataHubTypesWithDocumentFormat))]
    public async Task When_unused_datahub_value_then_exception_is_thrown(Type dataHubTypeWithUnused, DocumentFormat documentFormat)
    {
        ArgumentNullException.ThrowIfNull(dataHubTypeWithUnused);
        ArgumentNullException.ThrowIfNull(documentFormat);

        string unusedCode;

        var builder = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(ActorRole.EnergySupplier);

        if (dataHubTypeWithUnused == typeof(BusinessReason))
        {
            unusedCode = "A47";
            builder.WithBusinessReason(BusinessReason.FromCodeOrUnused(unusedCode));
        }
        else if (dataHubTypeWithUnused == typeof(SettlementVersion))
        {
            unusedCode = "D10";
            builder.WithSettlementVersion(SettlementVersion.FromCodeOrUnused(unusedCode));
        }
        else
        {
            throw new NotImplementedException($"Type {dataHubTypeWithUnused.Name} is not implemented yet");
        }

        var message = _energyResultMessageDtoBuilder.Build();

        var act = async () =>
        {
            await EnqueueMessage(message);
            var result = await PeekMessageAsync(MessageCategory.Aggregations, message.ReceiverNumber, message.ReceiverRole, documentFormat: documentFormat);
            return result;
        };

        (await act.Should().ThrowAsync<InvalidOperationException>($"because {unusedCode} is a unused {dataHubTypeWithUnused.Name} code"))
            .WithMessage($"{unusedCode} is not a valid {dataHubTypeWithUnused.Name}*");
    }

    [Fact]
    public async Task Given_OutgoingMessage_When_MessageIsPeeked_Then_MessageIsArchivedWithCorrectData()
    {
        // Arrange / Given
        var expectedEventId = EventId.From(Guid.NewGuid());
        var receiverNumber = SampleData.NewEnergySupplierNumber;
        var outgoingMessage = _energyResultMessageDtoBuilder
            .WithReceiverNumber(receiverNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .WithEventId(expectedEventId)
            .Build();

        await EnqueueMessage(outgoingMessage);

        var year = 2023;
        var month = 1;
        var day = 1;
        var hour = 13;
        var minute = 37;
        var expectedTimestamp = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        _dateTimeProvider.SetNow(expectedTimestamp.ToInstant());

        // Act / When
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations, null, null, DocumentFormat.Ebix);
        var sr = new StreamReader(peekResult.Bundle!);
        var txt = await sr.ReadToEndAsync();

        // Assert / Then
        peekResult.Should().NotBeNull("because a peek result should be found");
        peekResult.MessageId.Should().NotBeNull("because a peek result with a message id should be found");

        var archivedMessage = await GetArchivedMessageFromDatabaseAsync(peekResult.MessageId!);
        ((object?)archivedMessage).Should().NotBeNull("because an archived message should exists");

        var expectedFileStorageReference = $"{receiverNumber}/{year:0000}/{month:00}/{day:00}/{archivedMessage!.Id:N}";
        var assertProperties = new Dictionary<string, Action<object?>>
        {
            { "BusinessReason", businessReason => businessReason.Should().Be(outgoingMessage.BusinessReason) },
            { "CreatedAt", createdAt => createdAt.Should().Be(expectedTimestamp) },
            { "DocumentType", documentType => documentType.Should().Be(outgoingMessage.DocumentType.Name) },
            { "EventIds", eventIds => eventIds.Should().Be(expectedEventId.Value) },
            { "FileStorageReference", fileStorageReference => fileStorageReference.Should().Be(expectedFileStorageReference) },
            { "Id", id => id.Should().NotBeNull() },
            { "MessageId", messageId => messageId.Should().Be(peekResult.MessageId!.ToString()) },
            { "ReceiverNumber", receiverNumber => receiverNumber.Should().Be(outgoingMessage.ReceiverNumber.Value) },
            { "RecordId", recordId => recordId.Should().NotBeNull() },
            { "RelatedToMessageId", relatedToMessageId => relatedToMessageId.Should().BeNull() },
            { "SenderNumber", senderNumber => senderNumber.Should().Be(outgoingMessage.SenderId.Value) },
        };

        using var assertionScope = new AssertionScope();
        var archivedMessageAsDictionary = (IDictionary<string, object>)archivedMessage!;

        foreach (var assertProperty in assertProperties)
            assertProperty.Value(archivedMessageAsDictionary[assertProperty.Key]);

        assertProperties.Should().HaveSameCount(archivedMessageAsDictionary, "because all archived message properties should be asserted");

        foreach (var dbPropertyName in archivedMessageAsDictionary.Keys)
            assertProperties.Keys.Should().Contain(dbPropertyName);
    }

    [Fact]
    public async Task Given_OutgoingMessage_When_MessageIsPeeked_WithEbix_Then_MessageIsArchivedOnExpectedMessageId()
    {
        // Arrange / Given
        var expectedEventId = EventId.From(Guid.NewGuid());
        var receiverNumber = SampleData.NewEnergySupplierNumber;
        var outgoingMessage = _energyResultMessageDtoBuilder
            .WithReceiverNumber(receiverNumber)
            .WithReceiverRole(ActorRole.EnergySupplier)
            .WithEventId(expectedEventId)
            .Build();

        await EnqueueMessage(outgoingMessage);

        var year = 2023;
        var month = 1;
        var day = 1;
        var hour = 13;
        var minute = 37;
        var expectedTimestamp = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        _dateTimeProvider.SetNow(expectedTimestamp.ToInstant());

        // Act / When
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations, null, null, DocumentFormat.Ebix);
        var peekResult2 = await PeekMessageAsync(MessageCategory.Aggregations, null, null, DocumentFormat.Ebix);
        var ebixAsserter = NotifyAggregatedMeasureDataDocumentAsserter.CreateEbixAsserter(peekResult.Bundle!);
        var messageId = ebixAsserter.GetElement("HeaderEnergyDocument/Identification")!.LastNode?.ToString();

        // dequeue with -
        // Assert / Then
        peekResult.MessageId.Should().Be(messageId);

        var archivedMessage = await GetArchivedMessageFromDatabaseAsync(messageId!);
        ((object?)archivedMessage).Should().NotBeNull("because an archived message should exists");
        var outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        var dres = await outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(messageId!, ActorRole.EnergySupplier, ActorNumber.Create("5790000555551")), CancellationToken.None);
        //var dres = await outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(Guid.Parse(messageId!).ToString(), ActorRole.EnergySupplier, ActorNumber.Create("5790000555551")), CancellationToken.None);
        dres.Success.Should().BeTrue();
    }

    private async Task<bool> BundleIsRegistered()
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var numberOfBundles = await connection
            .ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Bundles");
        return numberOfBundles == 1;
    }

    private async Task<bool> MarketDocumentExists(string marketDocumentBundleId)
    {
        var sqlStatement =
            $"SELECT COUNT(*) FROM [dbo].[MarketDocuments] WHERE BundleId = '{marketDocumentBundleId}'";
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var exists = await connection.ExecuteScalarAsync<bool>(sqlStatement);

        return exists;
    }

    private async Task EnqueueMessage(EnergyResultMessageDto message)
    {
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
    }
}
