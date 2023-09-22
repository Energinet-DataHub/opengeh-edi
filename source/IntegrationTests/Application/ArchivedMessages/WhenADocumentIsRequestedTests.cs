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
using Energinet.DataHub.EDI.Application.ArchivedMessages;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Application.SearchMessages;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.ArchivedMessages;
using Energinet.DataHub.EDI.Domain.Common;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.ArchivedMessages;

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

    [Fact]
    public async Task Can_add_archived_messages_with_existing_message_id()
    {
        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();
        var messageId = "MessageId";
        await ArchiveMessage(CreateArchivedMessage(id1, messageId));

        try
        {
            await ArchiveMessage(CreateArchivedMessage(id2, messageId));
        }
#pragma warning disable CA1031  // We want to catch all exceptions
        catch
#pragma warning restore CA1031
        {
            Assert.Fail("We should be able to save multiple messages with the same message id");
        }

        var result = await QueryAsync(new GetMessagesQuery()).ConfigureAwait(false);

        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(messageId, result.Messages[0].MessageId);
        Assert.Equal(messageId, result.Messages[1].MessageId);
    }

    private ArchivedMessage CreateArchivedMessage(string? id = null, string? messageId = null)
    {
        return new ArchivedMessage(
            string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id,
            string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId,
            DocumentType.AccountingPointCharacteristics.Name,
            "1234512345123",
            "1234512345128",
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
