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

using System.Diagnostics;
using System.Reflection;
using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests;

[Collection(nameof(ArchivedMessagesCollection))]
public class SearchMeteringPointArchivedMessagesTests : IAsyncLifetime
{
    private readonly IArchivedMessagesClient _sut;
    private readonly ArchivedMessagesFixture _fixture;

    private readonly ActorIdentity _authenticatedActor = new(
        actorNumber: ActorNumber.Create("1234512345811"),
        restriction: Restriction.None,
        actorRole: ActorRole.MeteredDataAdministrator,
        actorClientId: null,
        actorId: Guid.Parse("00000000-0000-0000-0000-000000000001"));

    public SearchMeteringPointArchivedMessagesTests(ArchivedMessagesFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;

        var services = _fixture.BuildService(testOutputHelper);

        services.GetRequiredService<AuthenticatedActor>().SetAuthenticatedActor(_authenticatedActor);
        _sut = services.GetRequiredService<IArchivedMessagesClient>();
    }

    public static TheoryData<FieldToSortByDto, DirectionToSortByDto> GetAllCombinationOfFieldsToSortByAndDirectionsToSortBy()
    {
        var fieldsToSortBy =
            typeof(FieldToSortByDto).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var directionsToSortBy =
            typeof(DirectionToSortByDto).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        var theoryData = new TheoryData<FieldToSortByDto, DirectionToSortByDto>();
        foreach (var field in fieldsToSortBy)
        {
            var fieldValue = (FieldToSortByDto)field.GetValue(null)!;

            // Skip the DocumentType field, as it is not a valid field to sort by since it uses tinyInt
            if (fieldValue.Equals(FieldToSortByDto.DocumentType)) continue;

            foreach (var direction in directionsToSortBy)
            {
                var directionValue = (DirectionToSortByDto)direction.GetValue(null)!;
                theoryData.Add(fieldValue, directionValue);
            }
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

    [Fact]
    public async Task Given_ArchivedMessagesInStorage_When_GettingMessage_Then_StreamExists()
    {
        // Arrange
        var archivedMessage = await _fixture.CreateArchivedMessageAsync(
            documentType: DocumentType.NotifyValidatedMeasureData); // MeteredData DocumentType

        // Act
        var result = await _sut.GetAsync(archivedMessage.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Stream.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_ArchivedMessage_When_SearchingWithoutCriteria_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var meteringPointId = MeteringPointId.From("1234567890123");
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        var archivedMessage = await _fixture.CreateArchivedMessageAsync(
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto(),
                MeteringPointId: meteringPointId,
                CreationPeriod: new MessageCreationPeriodDto(now.Minus(Duration.FromDays(1)), now.Plus(Duration.FromDays(1)))),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmountOfMessages.Should().Be(1);
        using var assertionScope = new AssertionScope();
        var message = result.Messages.Should().ContainSingle().Subject;
        message.MessageId.Should().Be(archivedMessage.MessageId);
        message.SenderNumber.Should()
            .Be(archivedMessage.SenderNumber.Value)
            .And.NotBe(_authenticatedActor.ActorNumber.Value);
        message.SenderRoleCode.Should()
            .Be(archivedMessage.SenderRole.Code);
        message.ReceiverNumber.Should()
            .Be(archivedMessage.ReceiverNumber.Value)
            .And.NotBe(_authenticatedActor.ActorNumber.Value);
        message.ReceiverRoleCode.Should()
            .Be(archivedMessage.ReceiverRole.Code);
        message.DocumentType.Should().Be(archivedMessage.DocumentType.Name);
        message.BusinessReason.Should().Be(archivedMessage.BusinessReason?.Name);
        message.CreatedAt.Should().Be(archivedMessage.CreatedAt);
        message.Id.Should().Be(archivedMessage.Id.Value);
    }

    [Fact]
    public async Task Given_ThreeArchivedMessages_When_SearchingByDate_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedCreatedAt = CreatedAt("2023-05-01T22:00:00Z");
        var meteringPointId = MeteringPointId.From("1234567890123");
        await _fixture.CreateArchivedMessageAsync(
            timestamp: expectedCreatedAt.PlusDays(-1),
            documentType: DocumentType.NotifyValidatedMeasureData,
            meteringPointIds: [meteringPointId]);
        await _fixture.CreateArchivedMessageAsync(
            timestamp: expectedCreatedAt,
            documentType: DocumentType.NotifyValidatedMeasureData,
            meteringPointIds: [meteringPointId]);
        await _fixture.CreateArchivedMessageAsync(
            timestamp: expectedCreatedAt.PlusDays(1),
            documentType: DocumentType.NotifyValidatedMeasureData,
            meteringPointIds: [meteringPointId]);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto(),
                MeteringPointId: meteringPointId,
                CreationPeriod: new MessageCreationPeriodDto(
                    expectedCreatedAt.PlusHours(-2),
                    expectedCreatedAt.PlusHours(2))),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Messages.Should().ContainSingle()
            .Which.CreatedAt.Should().Be(expectedCreatedAt);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingBySenderNumber_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedSenderNumber = "9999999999999";
        var meteringPointId = MeteringPointId.From("1234567890123");
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        await _fixture.CreateArchivedMessageAsync(
            senderNumber: expectedSenderNumber,
            senderRole: ActorRole.MeteredDataResponsible,
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]); // MeteredData DocumentType
        await _fixture.CreateArchivedMessageAsync(
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]); // MeteredData DocumentType

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto(),
                MeteringPointId: meteringPointId,
                CreationPeriod: new MessageCreationPeriodDto(now.Minus(Duration.FromDays(1)), now.Plus(Duration.FromDays(1))),
                SenderNumber: expectedSenderNumber,
                SenderRoleCode: ActorRole.MeteredDataResponsible.Code),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.SenderNumber.Should().Be(expectedSenderNumber);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingBySenderRole_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedSenderRole = ActorRole.SystemOperator;
        var senderNumber = "9999999999999";
        var meteringPointId = MeteringPointId.From("1234567890123");
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        await _fixture.CreateArchivedMessageAsync(
            senderRole: expectedSenderRole,
            senderNumber: senderNumber,
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]);
        await _fixture.CreateArchivedMessageAsync(
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]);

        // Act
        var result = await _sut.SearchAsync(
        new GetMessagesQueryDto(
        new SortedCursorBasedPaginationDto(),
        MeteringPointId: meteringPointId,
        CreationPeriod: new MessageCreationPeriodDto(now.Minus(Duration.FromDays(1)), now.Plus(Duration.FromDays(1))),
        SenderRoleCode: expectedSenderRole.Code,
        SenderNumber: senderNumber),
        CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
        .Which.SenderRoleCode.Should().Be(expectedSenderRole.Code);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByReceiverRole_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedReceiverRole = ActorRole.SystemOperator;
        var receiverNumber = "9999999999999";
        var meteringPointId = MeteringPointId.From("1234567890123");
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        await _fixture.CreateArchivedMessageAsync(
            receiverRole: expectedReceiverRole,
            receiverNumber: receiverNumber,
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]);
        await _fixture.CreateArchivedMessageAsync(
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]);

        // Act
        var result = await _sut.SearchAsync(
        new GetMessagesQueryDto(
        new SortedCursorBasedPaginationDto(),
        MeteringPointId: meteringPointId,
        CreationPeriod: new MessageCreationPeriodDto(now.Minus(Duration.FromDays(1)), now.Plus(Duration.FromDays(1))),
        ReceiverRoleCode: expectedReceiverRole.Code,
        ReceiverNumber: receiverNumber),
        CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
        .Which.ReceiverRoleCode.Should().Be(expectedReceiverRole.Code);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingBySenderRoleAndReceiverRole_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedSenderRole = ActorRole.Delegated;
        var expectedReceiverRole = ActorRole.SystemOperator;
        var senderNumber = "9999999999999";
        var receiverNumber = "8888888888888";
        var meteringPointId = MeteringPointId.From("1234567890123");
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        await _fixture.CreateArchivedMessageAsync(
            senderRole: expectedSenderRole,
            senderNumber: senderNumber,
            receiverRole: expectedReceiverRole,
            receiverNumber: receiverNumber,
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]);
        await _fixture.CreateArchivedMessageAsync(
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]);

        // Act
        var result = await _sut.SearchAsync(
        new GetMessagesQueryDto(
        new SortedCursorBasedPaginationDto(),
        MeteringPointId: meteringPointId,
        CreationPeriod: new MessageCreationPeriodDto(now.Minus(Duration.FromDays(1)), now.Plus(Duration.FromDays(1))),
        ReceiverRoleCode: expectedReceiverRole.Code,
        ReceiverNumber: receiverNumber),
        CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.Should().Match<MessageInfoDto>(messageInfo =>
                messageInfo.SenderRoleCode == expectedSenderRole.Code
                && messageInfo.ReceiverRoleCode == expectedReceiverRole.Code);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByReceiverNumber_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedReceiverNumber = "9999999999999";
        var meteringPointId = MeteringPointId.From("1234567890123");
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        await _fixture.CreateArchivedMessageAsync(
            receiverNumber: expectedReceiverNumber,
            receiverRole: ActorRole.MeteredDataAdministrator,
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]);
        await _fixture.CreateArchivedMessageAsync(
            documentType: DocumentType.NotifyValidatedMeasureData,
            timestamp: now,
            meteringPointIds: [meteringPointId]);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto(),
                MeteringPointId: meteringPointId,
                CreationPeriod: new MessageCreationPeriodDto(now.Minus(Duration.FromDays(1)), now.Plus(Duration.FromDays(1))),
                ReceiverNumber: expectedReceiverNumber,
                ReceiverRoleCode: ActorRole.MeteredDataAdministrator.Code),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.ReceiverNumber.Should().Be(expectedReceiverNumber);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByDocumentType_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedDocumentType = DocumentType.NotifyValidatedMeasureData; // MeteredData DocumentType;
        var unexpectedDocumentType = DocumentType.RejectRequestAggregatedMeasureData;
        var meteringPointId = MeteringPointId.From("1234567890123");
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        await _fixture.CreateArchivedMessageAsync(
            documentType: expectedDocumentType,
            timestamp: now,
            meteringPointIds: [meteringPointId]);
        await _fixture.CreateArchivedMessageAsync(
            documentType: unexpectedDocumentType,
            timestamp: now,
            meteringPointIds: [meteringPointId]);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto(),
                MeteringPointId: meteringPointId,
                CreationPeriod: new MessageCreationPeriodDto(now.Minus(Duration.FromDays(1)), now.Plus(Duration.FromDays(1))),
                DocumentTypes: [expectedDocumentType.Name]),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.DocumentType.Should().Be(expectedDocumentType.Name);
    }

    [Fact]
    public async Task Given_ThreeArchivedMessages_When_SearchingByDocumentTypes_Then_ReturnsExpectedMessages()
    {
        // Arrange
        var expectedDocumentType1 = DocumentType.NotifyValidatedMeasureData;
        var expectedDocumentType2 = DocumentType.Acknowledgement;
        var unexpectedDocumentType = DocumentType.RejectRequestAggregatedMeasureData;
        var meteringPointId = MeteringPointId.From("1234567890123");
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);
        await _fixture.CreateArchivedMessageAsync(
            documentType: expectedDocumentType1,
            timestamp: now,
            meteringPointIds: [meteringPointId]);
        await _fixture.CreateArchivedMessageAsync(
            documentType: expectedDocumentType2,
            timestamp: now,
            meteringPointIds: [meteringPointId]);
        await _fixture.CreateArchivedMessageAsync(
            documentType: unexpectedDocumentType,
            timestamp: now,
            meteringPointIds: [meteringPointId]);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto(),
                MeteringPointId: meteringPointId,
                CreationPeriod: new MessageCreationPeriodDto(now.Minus(Duration.FromDays(1)), now.Plus(Duration.FromDays(1))),
                DocumentTypes:
                [
                    expectedDocumentType1.Name,
                    expectedDocumentType2.Name,
                ]),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().HaveCount(2);
        result.Messages.Select(message => message.DocumentType)
            .Should()
            .BeEquivalentTo(
            [
                expectedDocumentType1.Name,
                expectedDocumentType2.Name,
            ]);
    }

    #region pagination
    [Fact]
    public async Task Given_SevenArchivedMessages_When_NavigatingForwardIsTrue_Then_ExpectedMessagesAreReturned()
    {
        // Arrange
        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-07T22:00:00Z"), Guid.NewGuid().ToString()),
        };
        var meteringPointId = MeteringPointId.From("1234567890123");
        foreach (var messageToCreate in messages.OrderBy(_ => Random.Shared.Next()))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                documentType: DocumentType.NotifyValidatedMeasureData,
                meteringPointIds: [meteringPointId]);
        }

        var pagination = new SortedCursorBasedPaginationDto(
            PageSize: 10,
            NavigationForward: true);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                pagination,
                MeteringPointId: meteringPointId,
                CreationPeriod: new MessageCreationPeriodDto(CreatedAt("2023-04-01T20:00:00Z"), CreatedAt("2023-04-08T22:00:00Z"))),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(messages.Count);

        var expectedMessageIds = messages
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.MessageId)
            .ToList();
        result.Messages.Select(x => x.MessageId).Should().Equal(expectedMessageIds);
    }

    [Fact]
    public async Task Given_SevenArchivedMessages_When_NavigatingBackwardIsTrue_Then_ExpectedMessagesAreReturned()
    {
        // Arrange
        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-07T22:00:00Z"), Guid.NewGuid().ToString()),
        };
        var meteringPointId = MeteringPointId.From("1234567890123");
        foreach (var messageToCreate in messages.OrderBy(_ => Random.Shared.Next()))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                documentType: DocumentType.NotifyValidatedMeasureData,
                meteringPointIds: [meteringPointId]);
        }

        var pagination = new SortedCursorBasedPaginationDto(
            PageSize: 10,
            NavigationForward: false);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                pagination,
                MeteringPointId: meteringPointId,
                CreationPeriod: new MessageCreationPeriodDto(CreatedAt("2023-04-01T20:00:00Z"), CreatedAt("2023-04-08T22:00:00Z"))),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(messages.Count);

        var expectedMessageIds = messages
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.MessageId)
            .ToList();
        result.Messages.Select(x => x.MessageId).Should().Equal(expectedMessageIds);
    }

    [Theory]
    [InlineData(-8)]
    [InlineData(-1)]
    [InlineData(0)]
    public Task Given_SevenArchivedMessages_When_PageSizeIsInvalid_Then_ExceptionIsThrown(int pageSize)
    {
        // Arrange
        // Act
        var act = () => new SortedCursorBasedPaginationDto(PageSize: pageSize, NavigationForward: true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Page size must be a positive number. (Parameter 'pageSize')");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Given_SevenArchivedMessages_When_NavigatingForwardIsTrueAndSecondPage_Then_ExpectedMessagesAreReturned()
    {
        // Arrange
        var pageSize = 2;
        var pageNumber = 2;

        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            // page 1 <- the previous page
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()), // <- cursor points here
            // page 2 <- the page to fetch
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            // page 3
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
        };

        var meteringPointId = MeteringPointId.From("1234567890123");
        // Create messages in order when they were created at
        foreach (var messageToCreate in messages.OrderBy(x => x.CreatedAt))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                documentType: DocumentType.NotifyValidatedMeasureData,
                meteringPointIds: [meteringPointId]);
        }

        var messageCreationPeriodDto = new MessageCreationPeriodDto(CreatedAt("2023-04-01T20:00:00Z"), CreatedAt("2023-04-08T22:00:00Z"));
        var firstPageMessages = await SkipFirstPage(
            pageSize,
            navigatingForward: true,
            meteringPointId,
            messageCreationPeriodDto);
        // The cursor points at the last item of the previous page, when navigating backward
        var lastMessageInOnThePreviousPage = firstPageMessages.Messages.Last();
        var cursor = new SortingCursorDto(RecordId: lastMessageInOnThePreviousPage.RecordId);

        var pagination = new SortedCursorBasedPaginationDto(
            Cursor: cursor,
            PageSize: pageSize,
            NavigationForward: true);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                pagination,
                MeteringPointId: meteringPointId,
                CreationPeriod: messageCreationPeriodDto),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(pageSize);

        var expectedMessageIds = messages
            // Default sorting is descending by CreatedAt
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageSize * pageNumber) - pageSize)
            .Take(pageSize)
            .Select(x => x.MessageId)
            .ToList();

        result.Messages.Select(x => x.MessageId).Should().Equal(expectedMessageIds);
        result.TotalAmountOfMessages.Should().Be(6);
    }

    [Fact]
    public async Task Given_SevenArchivedMessages_When_NavigatingBackwardIsTrueAndSecondPage_Then_ExpectedMessagesAreReturned()
    {
        // Arrange
        var pageSize = 2;
        var pageNumber = 2;

        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            // page 1
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()),
            // page 2 <- the page to fetch
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            // page 3 <- the previous page
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()), // <- cursor points here
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
        };

        var meteringPointId = MeteringPointId.From("1234567890123");
        // Create messages in order when they were created at
        foreach (var messageToCreate in messages.OrderBy(x => x.CreatedAt))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                documentType: DocumentType.NotifyValidatedMeasureData,
                meteringPointIds: [meteringPointId]);
        }

        var messageCreationPeriodDto = new MessageCreationPeriodDto(CreatedAt("2023-04-01T20:00:00Z"), CreatedAt("2023-04-08T22:00:00Z"));
        var firstPageMessages = await SkipFirstPage(
            pageSize,
            navigatingForward: false,
            meteringPointId,
            messageCreationPeriodDto);
        // The cursor points at the first item of the previous page, when navigating backward
        var firstMessageInOnThePreviousPage = firstPageMessages.Messages.First();
        var cursor = new SortingCursorDto(RecordId: firstMessageInOnThePreviousPage.RecordId);

        var pagination = new SortedCursorBasedPaginationDto(
            Cursor: cursor,
            PageSize: pageSize,
            NavigationForward: false);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                pagination,
                MeteringPointId: meteringPointId,
                CreationPeriod: messageCreationPeriodDto),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(pageSize);

        var expectedMessageIds = messages
            .Skip((pageSize * pageNumber) - pageSize)
            .Take(pageSize)
            // Default sorting is descending by CreatedAt
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.MessageId)
            .ToList();

        result.Messages.Select(x => x.MessageId).Should().Equal(expectedMessageIds);
        result.TotalAmountOfMessages.Should().Be(6);
    }

    [Theory]
    [MemberData(nameof(GetAllCombinationOfFieldsToSortByAndDirectionsToSortBy))]
    public async Task Given_SevenArchivedMessages_When_NavigatingForwardIsTrueAndSortByField_Then_ExpectedMessagesAreReturned(
        FieldToSortByDto sortedBy,
        DirectionToSortByDto sortedDirection)
    {
        // Arrange
        var messages =
            new List<(Instant CreatedAt, string MessageId, string Sender, string Receiver, DocumentType DocumentType)>()
            {
                new(
                    CreatedAt("2023-04-01T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345128",
                    "1234512345122",
                    DocumentType.NotifyValidatedMeasureData),
                new(
                    CreatedAt("2023-04-02T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345127",
                    "1234512345123",
                    DocumentType.NotifyValidatedMeasureData),
                new(
                    CreatedAt("2023-04-03T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345125",
                    "1234512345121",
                    DocumentType.NotifyValidatedMeasureData),
                new(
                    CreatedAt("2023-04-04T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345123",
                    "1234512345126",
                    DocumentType.Acknowledgement),
                new(
                    CreatedAt("2023-04-05T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345123",
                    "1234512345128",
                    DocumentType.NotifyValidatedMeasureData),
                new(
                    CreatedAt("2023-04-06T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345122",
                    "1234512345128",
                    DocumentType.Acknowledgement),
            };
        var meteringPointId = MeteringPointId.From("1234567890123");
        var recordIdsForMessages = new Dictionary<string, int>();
        var recordId = 0;
        foreach (var messageToCreate in messages.OrderBy(_ => Random.Shared.Next()))
        {
            recordIdsForMessages.Add(messageToCreate.MessageId, recordId++);
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                documentType: messageToCreate.DocumentType,
                senderNumber: messageToCreate.Sender,
                receiverNumber: messageToCreate.Receiver,
                meteringPointIds: [meteringPointId]);
        }

        var pagination = new SortedCursorBasedPaginationDto(
            PageSize: messages.Count,
            NavigationForward: true,
            FieldToSortBy: sortedBy,
            DirectionToSortBy: sortedDirection);

        // Act
        var result = await _sut.SearchAsync(
                new GetMessagesQueryDto(
                    pagination,
                    MeteringPointId: meteringPointId,
                    CreationPeriod: new MessageCreationPeriodDto(CreatedAt("2023-04-01T20:00:00Z"), CreatedAt("2023-04-08T22:00:00Z"))),
                CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(messages.Count);
        var orderedMessages = GetSortedMessaged(sortedBy, sortedDirection, messages, recordIdsForMessages);

        result.Messages.Select(x => x.MessageId)
            .Should()
            .Equal(orderedMessages.Select(x => x.MessageId));
    }

    [Theory]
    [MemberData(nameof(GetAllCombinationOfFieldsToSortByAndDirectionsToSortBy))]
    public async Task
        Given_SevenArchivedMessages_When_NavigatingBackwardIsTrueAndSortByField_Then_ExpectedMessagesAreReturned(
            FieldToSortByDto sortedBy,
            DirectionToSortByDto sortedDirection)
    {
        // Arrange
        var messages =
            new List<(Instant CreatedAt, string MessageId, string Sender, string Receiver, DocumentType DocumentType)>()
            {
                new(
                    CreatedAt("2023-04-01T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345128",
                    "1234512345122",
                    DocumentType.NotifyValidatedMeasureData),
                new(
                    CreatedAt("2023-04-02T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345127",
                    "1234512345123",
                    DocumentType.Acknowledgement),
                new(
                    CreatedAt("2023-04-03T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345125",
                    "1234512345121",
                    DocumentType.NotifyValidatedMeasureData),
                new(
                    CreatedAt("2023-04-04T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345123",
                    "1234512345126",
                    DocumentType.Acknowledgement),
                new(
                    CreatedAt("2023-04-05T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345123",
                    "1234512345128",
                    DocumentType.NotifyValidatedMeasureData),
                new(
                    CreatedAt("2023-04-06T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345122",
                    "1234512345128",
                    DocumentType.Acknowledgement),
            };
        var meteringPointId = MeteringPointId.From("1234567890123");
        var recordIdsForMessages = new Dictionary<string, int>();
        var recordId = 0;
        foreach (var messageToCreate in messages.OrderBy(_ => Random.Shared.Next()))
        {
            recordIdsForMessages.Add(messageToCreate.MessageId, recordId++);
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                documentType: messageToCreate.DocumentType,
                senderNumber: messageToCreate.Sender,
                receiverNumber: messageToCreate.Receiver,
                meteringPointIds: [meteringPointId]);
        }

        var pagination = new SortedCursorBasedPaginationDto(
            PageSize: messages.Count,
            NavigationForward: false,
            FieldToSortBy: sortedBy,
            DirectionToSortBy: sortedDirection);

        // Act
        var result = await _sut.SearchAsync(
                new GetMessagesQueryDto(
                    pagination,
                    MeteringPointId: meteringPointId,
                    CreationPeriod: new MessageCreationPeriodDto(CreatedAt("2023-04-01T00:00:00Z"), CreatedAt("2023-04-08T22:00:00Z"))),
                CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(messages.Count);
        var orderedMessages = GetSortedMessaged(sortedBy, sortedDirection, messages, recordIdsForMessages);

        result.Messages.Select(x => x.MessageId)
            .Should()
            .Equal(orderedMessages.Select(x => x.MessageId), $"Message is sorted by {sortedBy.Identifier} {sortedDirection.Identifier}");
    }

    [Fact]
    public async Task Given_ArchivedMessages_When_OneIsOlderThenSearchCriteriaAndNavigatingForwardIsTrue_Then_ExpectedAmountOfMessagesAreReturned()
    {
        // Arrange
        var expectedPeriodStartedAt = CreatedAt("2023-04-02T22:00:00Z");
        var expectedPeriodEndedAt = CreatedAt("2023-04-06T22:00:00Z");
        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-07T22:00:00Z"), Guid.NewGuid().ToString()),
        };
        var meteringPointId = MeteringPointId.From("1234567890123");
        foreach (var messageToCreate in messages.OrderBy(_ => Random.Shared.Next()))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                documentType: DocumentType.Acknowledgement,
                meteringPointIds: [meteringPointId]);
        }

        var pagination = new SortedCursorBasedPaginationDto(
            PageSize: 10,
            NavigationForward: true);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                pagination,
                new MessageCreationPeriodDto(
                expectedPeriodStartedAt,
                expectedPeriodEndedAt),
                MeteringPointId: meteringPointId),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(5);
        result.TotalAmountOfMessages.Should().Be(5);
    }

    [Fact]
    public async Task Given_SecondPageIsRequest_When_NavigatingForwardIsTrueAndSortingOnSenderAndAMessageForADifferentSenderExistsWithPeriodSearchCritieria_Then_ExpectedMessageAreReturned()
    {
        // Arrange
        var expectedPeriodStartedAt = CreatedAt("2023-04-02T22:00:00Z");
        var expectedPeriodEndedAt = CreatedAt("2023-04-06T22:00:00Z");
        var pageSize = 3;
        var pageNumber = 2;

        var messages = new List<(Instant CreatedAt, string MessageId, string SenderNumber)>()
        {
            // page 1 <- the previous page
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345123"),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345123"), // <- cursor points here
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345123"),
            // page 2
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345123"),
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345123"),
            // has a different sender
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345122"),
        };

        var meteringPointId = MeteringPointId.From("1234567890123");
        // Create messages in order when they were created at
        foreach (var messageToCreate in messages.OrderBy(x => x.CreatedAt))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                senderNumber: messageToCreate.SenderNumber,
                documentType: DocumentType.Acknowledgement,
                meteringPointIds: [meteringPointId]);
        }

        var messageCreationPeriodDto = new MessageCreationPeriodDto(expectedPeriodStartedAt, expectedPeriodEndedAt);
        var firstPageMessages = await SkipFirstPage(
            pageSize,
            navigatingForward: true,
            meteringPointId,
            messageCreationPeriodDto,
            orderByField: FieldToSortByDto.SenderNumber);
        // The cursor points at the last item of the previous page, when navigating backward
        var lastMessageInOnThePreviousPage = firstPageMessages.Messages.Last();
        var cursor = new SortingCursorDto(RecordId: lastMessageInOnThePreviousPage.RecordId, SortedFieldValue: lastMessageInOnThePreviousPage.SenderNumber);

        var pagination = new SortedCursorBasedPaginationDto(
            Cursor: cursor,
            PageSize: pageSize,
            NavigationForward: true,
            FieldToSortBy: FieldToSortByDto.SenderNumber,
            DirectionToSortBy: DirectionToSortByDto.Descending);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                pagination,
                messageCreationPeriodDto,
                MeteringPointId: meteringPointId),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(pageNumber);
        result.TotalAmountOfMessages.Should().Be(5);
    }

    [Fact]
    public async Task Given_10000ArchivedMessagesSearch_When_GettingFirstPage_Then_ShouldCompleteWithinExpectedTime()
    {
        // Arrange
        const int totalMessages = 10000;
        var meteringPointId = MeteringPointId.From("1234567890123");
        var now = Instant.FromUtc(2024, 05, 07, 13, 37);

        // Seed 10,000 messages
        for (var i = 0; i < totalMessages; i++)
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: now.Plus(Duration.FromSeconds(i)),
                messageId: Guid.NewGuid().ToString(),
                documentType: DocumentType.NotifyValidatedMeasureData,
                meteringPointIds: [meteringPointId]);
        }

        var query = new GetMessagesQueryDto(
            new SortedCursorBasedPaginationDto(PageSize: 20, NavigationForward: true),
            MeteringPointId: meteringPointId,
            CreationPeriod: new MessageCreationPeriodDto(now.Minus(Duration.FromDays(1)), now.Plus(Duration.FromDays(1))));

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _sut.SearchAsync(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.Messages.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Expect the search to complete within ½ a second
    }
    #endregion

    private static Instant CreatedAt(string date)
    {
        return InstantPattern.General.Parse(date).Value;
    }

    private static
        IOrderedEnumerable<(Instant CreatedAt, string MessageId, string Sender, string Receiver, DocumentType DocumentType)>
        GetSortedMessaged(
            FieldToSortByDto sortedBy,
            DirectionToSortByDto sortedDirection,
            List<(Instant CreatedAt, string MessageId, string Sender, string Receiver, DocumentType DocumentType)> messages,
            Dictionary<string, int> recordIdsForMessages)
    {
        var orderedMessages = messages.Order();
        if (sortedBy.Identifier == FieldToSortByDto.MessageId.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortByDto.Ascending.Identifier
                ? messages.OrderBy(x => x.MessageId)
                : messages.OrderByDescending(x => x.MessageId);
        }

        if (sortedBy.Identifier == FieldToSortByDto.CreatedAt.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortByDto.Ascending.Identifier
                ? messages.OrderBy(x => x.CreatedAt).ThenByDescending(x => recordIdsForMessages[x.MessageId])
                : messages.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => recordIdsForMessages[x.MessageId]);
        }

        if (sortedBy.Identifier == FieldToSortByDto.DocumentType.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortByDto.Ascending.Identifier
                ? messages.OrderBy(x => x.DocumentType).ThenByDescending(x => recordIdsForMessages[x.MessageId])
                : messages.OrderByDescending(x => x.DocumentType).ThenByDescending(x => recordIdsForMessages[x.MessageId]);
        }

        if (sortedBy.Identifier == FieldToSortByDto.SenderNumber.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortByDto.Ascending.Identifier
                ? messages.OrderBy(x => x.Sender).ThenByDescending(x => recordIdsForMessages[x.MessageId])
                : messages.OrderByDescending(x => x.Sender).ThenByDescending(x => recordIdsForMessages[x.MessageId]);
        }

        if (sortedBy.Identifier == FieldToSortByDto.ReceiverNumber.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortByDto.Ascending.Identifier
                ? messages.OrderBy(x => x.Receiver).ThenByDescending(x => recordIdsForMessages[x.MessageId])
                : messages.OrderByDescending(x => x.Receiver).ThenByDescending(x => recordIdsForMessages[x.MessageId]);
        }

        return orderedMessages;
    }

    private async Task<MessageSearchResultDto> SkipFirstPage(
        int pageSize,
        bool navigatingForward,
        MeteringPointId meteringPointId,
        MessageCreationPeriodDto creationPeriod,
        FieldToSortByDto? orderByField = null)
    {
        var pagination = new SortedCursorBasedPaginationDto(PageSize: pageSize, NavigationForward: navigatingForward, FieldToSortBy: orderByField, DirectionToSortBy: DirectionToSortByDto.Descending);
        return await _sut.SearchAsync(
            new GetMessagesQueryDto(
                pagination,
                CreationPeriod: creationPeriod,
                MeteringPointId: meteringPointId),
            CancellationToken.None);
    }
}
