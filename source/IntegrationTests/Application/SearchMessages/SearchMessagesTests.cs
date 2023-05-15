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
using System.Linq;
using System.Threading.Tasks;
using Application.Configuration;
using Application.Configuration.DataAccess;
using Application.SearchMessages;
using Domain.Actors;
using Domain.ArchivedMessages;
using Domain.Documents;
using IntegrationTests.Fixtures;
using NodaTime;
using Xunit;

namespace IntegrationTests.Application.SearchMessages;

public class SearchMessagesTests : TestBase
{
    private readonly IArchivedMessageRepository _repository;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public SearchMessagesTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _repository = GetService<IArchivedMessageRepository>();
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
    }

    [Fact]
    public async Task Can_fetch_messages()
    {
        var archivedMessage = CreateArchivedMessage(_systemDateTimeProvider.Now());
        await ArchiveMessage(archivedMessage);

        var result = await QueryAsync(new GetMessagesQuery()).ConfigureAwait(false);

        var messageInfo = result.Messages.FirstOrDefault(message => message.MessageId == archivedMessage.Id);
        Assert.NotNull(messageInfo);
        Assert.Equal(archivedMessage.DocumentType.Name, messageInfo.DocumentType);
        Assert.Equal(archivedMessage.SenderNumber.Value, messageInfo.SenderNumber);
        Assert.Equal(archivedMessage.ReceiverNumber.Value, messageInfo.ReceiverNumber);
        Assert.Equal(archivedMessage.CreatedAt, messageInfo.CreatedAt);
    }

    [Fact]
    public async Task Filter_messages_by_creation_date_period()
    {
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-04-01T22:00:00Z")));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z")));

        var result = await QueryAsync(new GetMessagesQuery(new MessageCreationPeriod(
            CreatedAt("2023-05-01T22:00:00Z"),
            CreatedAt("2023-05-02T22:00:00Z")))).ConfigureAwait(false);

        Assert.Single(result.Messages);
        Assert.Equal(CreatedAt("2023-05-01T22:00:00Z"), result.Messages[0].CreatedAt);
    }

    [Fact]
    public async Task Filter_messages_by_message_id_and_created_date()
    {
        //Arrange
        var messageId = Guid.NewGuid();
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z"), messageId));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z")));

        //Act
        var result = await QueryAsync(new GetMessagesQuery(
            new MessageCreationPeriod(
            CreatedAt("2023-05-01T22:00:00Z"),
            CreatedAt("2023-05-02T22:00:00Z")),
            messageId)).ConfigureAwait(false);

        //Assert
        Assert.Equal(messageId, result.Messages[0].MessageId);
    }

    [Fact]
    public async Task Filter_messages_by_message_id()
    {
        //Arrange
        var messageId = Guid.NewGuid();
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z"), messageId));

        //Act
        var result = await QueryAsync(new GetMessagesQuery(
            MessageId: messageId)).ConfigureAwait(false);

        //Assert
        Assert.Single(result.Messages);
        Assert.Equal(messageId, result.Messages[0].MessageId);
    }

    [Fact]
    public async Task Filter_messages_by_sender_number()
    {
        //Arrange
        var senderNumber = "1234512345128";
        await ArchiveMessage(CreateArchivedMessage(senderNumber: senderNumber));
        await ArchiveMessage(CreateArchivedMessage());

        //Act
        var result = await QueryAsync(new GetMessagesQuery(SenderNumber: senderNumber)).ConfigureAwait(false);

        //Assert
        Assert.Single(result.Messages);
        Assert.Equal(senderNumber, result.Messages[0].SenderNumber);
    }

    [Fact]
    public async Task Filter_messages_by_receiver()
    {
        // Arrange
        var receiverNumber = "1234512345129";
        await ArchiveMessage(CreateArchivedMessage(receiverNumber: receiverNumber));
        await ArchiveMessage(CreateArchivedMessage());

        // Act
        var result = await QueryAsync(new GetMessagesQuery(ReceiverNumber: receiverNumber)).ConfigureAwait(false);

        // Assert
        Assert.Single(result.Messages);
        Assert.Equal(receiverNumber, result.Messages[0].ReceiverNumber);
    }

    private static Instant CreatedAt(string date)
    {
        return NodaTime.Text.InstantPattern.General.Parse(date).Value;
    }

    private ArchivedMessage CreateArchivedMessage(Instant? createdAt = null, Guid? messageId = null, string? senderNumber = null, string? receiverNumber = null)
    {
        return new ArchivedMessage(
            messageId.GetValueOrDefault(Guid.NewGuid()),
            DocumentType.AccountingPointCharacteristics,
            ActorNumber.Create(senderNumber ?? "1234512345123"),
            ActorNumber.Create(receiverNumber ?? "1234512345128"),
            createdAt.GetValueOrDefault(_systemDateTimeProvider.Now()));
    }

    private async Task ArchiveMessage(ArchivedMessage archivedMessage)
    {
        _repository.Add(archivedMessage);
        await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
    }
}
