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

using Dapper;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Mapping;
using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests;

[Collection(nameof(ArchivedMessagesCollection))]
public class WhenArchivedMessageIsCreatedTests : IAsyncLifetime
{
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly ArchivedMessagesFixture _fixture;
    private readonly ActorIdentity _authenticatedActor = new(
        actorNumber: ActorNumber.Create("1234512345811"),
        restriction: Restriction.None,
        actorRole: ActorRole.MeteredDataAdministrator,
        actorClientId: null,
        actorId: Guid.Parse("00000000-0000-0000-0000-000000000001"));

    public WhenArchivedMessageIsCreatedTests(ArchivedMessagesFixture archivedMessagesFixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = archivedMessagesFixture;
        var services = _fixture.BuildService(testOutputHelper);
        services.GetRequiredService<AuthenticatedActor>().SetAuthenticatedActor(_authenticatedActor);
        _archivedMessagesClient = services.GetRequiredService<IArchivedMessagesClient>();
    }

    public static TheoryData<DocumentType> MeteringPointDocumentTypes => new()
    {
        DocumentType.NotifyValidatedMeasureData,
        DocumentType.Acknowledgement,
        DocumentType.RequestMeasurements,
        DocumentType.RejectRequestMeasurements,
    };

    public static TheoryData<DocumentType> GetDocumentTypes()
    {
        var theoryData = new TheoryData<DocumentType>();

        foreach (var documentType in EnumerationType.GetAll<DocumentType>())
        {
            theoryData.Add(documentType);
        }

        return theoryData;
    }

    public Task InitializeAsync()
    {
        _fixture.CleanupDatabase();
        _fixture.CleanupFileStorage();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(GetDocumentTypes))]
    public async Task Given_ArchivedDocument_When_Created_Then_CanBeRetrieved(DocumentType documentType)
    {
        var correctArchivedMessage = await CreateArchivedMessageAsync(documentType: documentType);

        var result = await _archivedMessagesClient.GetAsync(correctArchivedMessage.Id, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Theory]
    [MemberData(nameof(GetDocumentTypes))]
    public async Task Given_DocumentType_When_Created_Then_StoredInExpectedTable(DocumentType documentType)
    {
        await CreateArchivedMessageAsync(documentType: documentType);

        var numberOfArchivedMeteringPointMessages = await _fixture.GetNumberOfCreatedMeteringPointMessages();
        var numberOfArchivedMessages = await _fixture.GetNumberOfCreatedMessagesInDatabase();

        if (MeteringPointDocumentTypes.Contains(documentType))
        {
            numberOfArchivedMeteringPointMessages.Should().Be(1);
            numberOfArchivedMessages.Should().Be(0);
        }
        else
        {
            numberOfArchivedMeteringPointMessages.Should().Be(0);
            numberOfArchivedMessages.Should().Be(1);
        }
    }

    [Fact]
    public async Task Given_ArchivedDocument_When_Created_Then_RetrievedWithCorrectContent()
    {
        // Arrange
        var correctDocumentContent = "correct document content";
        var correctArchivedMessage = await CreateArchivedMessageAsync(documentContent: correctDocumentContent);
        await CreateArchivedMessageAsync(documentContent: "incorrect document content");

        // Act
        var result = await _archivedMessagesClient.GetAsync(correctArchivedMessage.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var streamReader = new StreamReader(result!.Stream);
        var actualDocumentContent = await streamReader.ReadToEndAsync();
        actualDocumentContent.Should().Be(correctDocumentContent);
    }

    [Theory]
    [InlineData(ArchivedMessageTypeDto.IncomingMessage)]
    [InlineData(ArchivedMessageTypeDto.OutgoingMessage)]
    public async Task Given_ArchivedDocument_When_Created_Then_IsSavedAtCorrectPath(ArchivedMessageTypeDto archivedMessageType)
    {
        var messageId = MessageId.New();
        var senderNumber = "1122334455667788";
        var receiverNumber = "8877665544332211";
        int year = 2024,
            month = 01,
            date = 25;

        var archivedMessage = await CreateArchivedMessageAsync(
            archivedMessageType: archivedMessageType,
            messageId: messageId.Value,
            senderNumber: senderNumber,
            receiverNumber: receiverNumber,
            timestamp: Instant.FromUtc(year, month, date, 0, 0));

        var mappedArchiveMessage = ArchivedMessageMapper.Map(archivedMessage);

        var expectedActorNumber = archivedMessageType == ArchivedMessageTypeDto.IncomingMessage ? senderNumber : receiverNumber;
        var expectedFileStorageReference = $"{expectedActorNumber}/{year:000}/{month:00}/{date:00}/{archivedMessage.Id.Value:N}";

        var actualFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageId.Value);

        using var assertionScope = new AssertionScope();
        mappedArchiveMessage.FileStorageReference.Category.Value.Should().Be("archived");
        actualFileStorageReference.Should().Be(expectedFileStorageReference);
    }

    [Fact]
    public async Task Given_ArchivedDocument_When_Created_Then_IdHasToBeUniqueInDatabase()
    {
        var serviceProviderWithoutFileStorage = BuildServiceProviderWithoutFileStorage();
        var clientWithoutFileStorage = serviceProviderWithoutFileStorage.GetRequiredService<IArchivedMessagesClient>();
        var archivedMessageDto = new ArchivedMessageDto(
            Guid.NewGuid().ToString(),
            new[] { EventId.From(Guid.NewGuid()) },
            DocumentType.NotifyAggregatedMeasureData,
            ActorNumber.Create("1234512345123"),
            ActorRole.EnergySupplier,
            ActorNumber.Create("1234512345128"),
            ActorRole.EnergySupplier,
            Instant.FromUtc(2023, 01, 01, 0, 0),
            BusinessReason.BalanceFixing,
            ArchivedMessageTypeDto.OutgoingMessage,
            new ArchivedMessageStreamDto(new MemoryStream()),
            Array.Empty<MeteringPointId>());
        await clientWithoutFileStorage.CreateAsync(archivedMessageDto, CancellationToken.None);
        var createDuplicateArchivedMessage = new Func<Task>(() => clientWithoutFileStorage.CreateAsync(archivedMessageDto, CancellationToken.None));

        await createDuplicateArchivedMessage.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task Given_ArchivedMessage_When_CreatedWithSameMessageId_Then_ArchivedMessageIsCreated()
    {
        var messageId = "MessageId";
        await CreateArchivedMessageAsync(messageId: messageId);
        await CreateArchivedMessageAsync(messageId: messageId);

        var result = await _archivedMessagesClient.SearchAsync(new GetMessagesQueryDto(new SortedCursorBasedPaginationDto()), CancellationToken.None);

        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(messageId, result.Messages[0].MessageId);
        Assert.Equal(messageId, result.Messages[1].MessageId);
    }

    [Theory]
    [MemberData(nameof(MeteringPointDocumentTypes))]
    public async Task Given_MeteringPointArchivedMessage_When_Creating_Then_MessageIsStoredInDatabaseAndBlob(DocumentType meteringPointDocumentType)
    {
        // Arrange
        var archivedMessage = await _fixture.CreateArchivedMessageAsync(
            archivedMessageType: ArchivedMessageTypeDto.IncomingMessage,
            documentType: meteringPointDocumentType,
            meteringPointIds: [MeteringPointId.From("1234567890123")],
            storeMessage: false);

        var messageCreatedAt = archivedMessage.CreatedAt.ToDateTimeUtc();

        // Since the message is an incoming message, we need the "senderNumber" in the path
        var expectedBlobPath = $"{archivedMessage.SenderNumber.Value}/"
                               + $"{messageCreatedAt.Year:0000}/"
                               + $"{messageCreatedAt.Month:00}/"
                               + $"{messageCreatedAt.Day:00}/"
                               + $"{archivedMessage.Id.Value:N}"; // remove dashes from guid

        var expectedBlobReference = new FileStorageReference(
            category: FileStorageCategory.ArchivedMessage(),
            path: expectedBlobPath);

        // Act
        await _archivedMessagesClient.CreateAsync(archivedMessage, CancellationToken.None);

        // Assert
        var dbResult = await _fixture.GetAllMeteringPointMessagesInDatabase();

        var message = dbResult.Single();
        using var assertionScope = new AssertionScope();

        message.SenderNumber.Should().Be(archivedMessage.SenderNumber.Value);
        message.SenderRole.Should().Be(archivedMessage.SenderRole.DatabaseValue);
        message.ReceiverNumber.Should().Be(archivedMessage.ReceiverNumber.Value);
        message.ReceiverRole.Should().Be(archivedMessage.ReceiverRole.DatabaseValue);
        message.DocumentType.Should().Be(archivedMessage.DocumentType.DatabaseValue);
        message.BusinessReason.Should().Be(archivedMessage.BusinessReason?.DatabaseValue);
        message.MessageId.Should().Be(archivedMessage.MessageId);
        message.FileStorageReference.Should().Be(expectedBlobReference.Path);
        message.RelatedToMessageId.Should().BeNull();
        message.EventIds.Should().BeNull();

        var blobResult = await _fixture.GetMessagesFromBlob(expectedBlobReference);
        blobResult.Should().NotBeNull();
    }

    private async Task<ArchivedMessageDto> CreateArchivedMessageAsync(
        ArchivedMessageTypeDto? archivedMessageType = null,
        string? messageId = null,
        string? documentContent = null,
        string? senderNumber = null,
        string? receiverNumber = null,
        DocumentType? documentType = null,
        Instant? timestamp = null)
    {
        var documentStream = new MemoryStream();

        if (!string.IsNullOrEmpty(documentContent))
        {
            // Don't dispose streamWriter, it disposes the documentStream as well
#pragma warning disable CA2000
            var streamWriter = new StreamWriter(documentStream);
#pragma warning restore CA2000
            streamWriter.Write(documentContent);
            streamWriter.Flush();
        }

        return await _fixture.CreateArchivedMessageAsync(
            archivedMessageType ?? ArchivedMessageTypeDto.OutgoingMessage,
            string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId,
            documentContent,
            documentType ?? DocumentType.NotifyAggregatedMeasureData,
            BusinessReason.BalanceFixing,
            ActorNumber.Create(senderNumber ?? "1234512345123").Value,
            ActorRole.EnergySupplier,
            ActorNumber.Create(receiverNumber ?? "1234512345128").Value,
            ActorRole.EnergySupplier,
            timestamp ?? Instant.FromUtc(2023, 01, 01, 0, 0));
    }

    private ServiceProvider BuildServiceProviderWithoutFileStorage()
    {
        var serviceCollectionWithoutFileStorage = _fixture.GetServiceCollectionClone();
        serviceCollectionWithoutFileStorage.RemoveAll<IFileStorageClient>();
        serviceCollectionWithoutFileStorage.AddScoped<IFileStorageClient, FileStorageClientStub>();

        var dependenciesWithoutFileStorage = serviceCollectionWithoutFileStorage.BuildServiceProvider();
        return dependenciesWithoutFileStorage;
    }

    private async Task<string?> GetArchivedMessageFileStorageReferenceFromDatabaseAsync(string messageId)
    {
        using var connection =
            await _fixture.Services.GetRequiredService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var fileStorageReference = await connection.ExecuteScalarAsync<string>(
            $"SELECT FileStorageReference FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

        return fileStorageReference;
    }
}
