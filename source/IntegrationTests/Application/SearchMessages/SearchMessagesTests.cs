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
        var archivedMessage = new ArchivedMessage(Guid.NewGuid(), DocumentType.AccountingPointCharacteristics, ActorNumber.Create("1234512345123"), ActorNumber.Create("1234512345124"), _systemDateTimeProvider.Now());
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
    public async Task Can_filter_by_creation_date_period()
    {
        var createdAt = NodaTime.Text.InstantPattern.General.Parse("2023-04-01T22:00:00Z").Value;
        var archivedMessage = CreateArchivedMessage(createdAt);
        await ArchiveMessage(archivedMessage);
        await ArchiveMessage(
            new ArchivedMessage(
                Guid.NewGuid(),
                DocumentType.AccountingPointCharacteristics,
                ActorNumber.Create("1234512345123"),
                ActorNumber.Create("1234512345124"),
                _systemDateTimeProvider.Now()));

        var startOfPeriod = NodaTime.Text.InstantPattern.General.Parse("2023-05-01T22:00:00Z").Value;
        var endOfPeriod = NodaTime.Text.InstantPattern.General.Parse("2023-05-02T22:00:00Z").Value;
        var result = await QueryAsync(new GetMessagesQuery(startOfPeriod, endOfPeriod)).ConfigureAwait(false);

        Assert.Single(result.Messages);
    }

    private static ArchivedMessage CreateArchivedMessage(Instant createdAt)
    {
        return new ArchivedMessage(Guid.NewGuid(), DocumentType.AccountingPointCharacteristics, ActorNumber.Create("1234512345123"), ActorNumber.Create("1234512345124"), createdAt);
    }

    private async Task ArchiveMessage(ArchivedMessage archivedMessage)
    {
        _repository.Add(archivedMessage);
        await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
    }
}
