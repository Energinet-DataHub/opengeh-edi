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
using System.IO;
using System.Threading.Tasks;
using Application.ArchivedMessages;
using Application.Configuration;
using Application.Configuration.DataAccess;
using Domain.Actors;
using Domain.ArchivedMessages;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.SeedWork;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests.Application.ArchivedMessages;

public class WhenADocumentIsRequestedTests : TestBase
{
    private readonly IArchivedMessageRepository _archivedMessageRepository;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public WhenADocumentIsRequestedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _archivedMessageRepository = GetService<IArchivedMessageRepository>();
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
    }

    [Fact]
    public async Task Get_document_by_id()
    {
        var id = Guid.NewGuid().ToString();
        await ArchiveMessage(CreateArchivedMessage());
        await ArchiveMessage(CreateArchivedMessage(id));
        await ArchiveMessage(CreateArchivedMessage());

        var result = await QueryAsync(new GetArchivedMessageDocumentQuery(id)).ConfigureAwait(false);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Id_has_to_be_unique()
    {
        var id = Guid.NewGuid().ToString();
        await ArchiveMessage(CreateArchivedMessage(id));

        await Assert.ThrowsAsync<InvalidOperationException>(() => ArchiveMessage(CreateArchivedMessage(id))).ConfigureAwait(false);
    }

    private ArchivedMessage CreateArchivedMessage(string? messageId = null)
    {
        return new ArchivedMessage(
            string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId,
            EnumerationType.FromName<DocumentType>(DocumentType.AccountingPointCharacteristics.Name),
            ActorNumber.Create("1234512345123"),
            ActorNumber.Create("1234512345128"),
            _systemDateTimeProvider.Now(),
            BusinessReason.BalanceFixing.Name,
            new MemoryStream());
    }

    private async Task ArchiveMessage(ArchivedMessage archivedMessage)
    {
        _archivedMessageRepository.Add(archivedMessage);
        await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
    }
}
