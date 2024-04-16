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
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;

namespace Energinet.DataHub.EDI.ArchivedMessages.Infrastructure;

public class ArchivedMessageRepository : IArchivedMessageRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly IFileStorageClient _fileStorageClient;

    public ArchivedMessageRepository(
        IDatabaseConnectionFactory connectionFactory,
        AuthenticatedActor authenticatedActor,
        IFileStorageClient fileStorageClient)
    {
        _connectionFactory = connectionFactory;
        _authenticatedActor = authenticatedActor;
        _fileStorageClient = fileStorageClient;
    }

    public async Task AddAsync(ArchivedMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Must upload to file storage before adding to database, to ensure the file cannot be added to the db without the file existing in file storage
        await _fileStorageClient.UploadAsync(message.FileStorageReference, message.ArchivedMessageStream.Stream).ConfigureAwait(false);

        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = @"INSERT INTO [dbo].[ArchivedMessages]
                       ([Id], [EventIds], [DocumentType], [ReceiverNumber], [SenderNumber], [CreatedAt], [BusinessReason], [FileStorageReference], [MessageId], [RelatedToMessageId])
                       VALUES
                       (@Id, @EventIds, @DocumentType, @ReceiverNumber, @SenderNumber, @CreatedAt, @BusinessReason, @FileStorageReference, @MessageId, @RelatedToMessageId)";

        var parameters = new
        {
            Id = message.Id.Value.ToString(),
            EventIds = message.EventIds.Count > 0 ? string.Join("::", message.EventIds.Select(id => id.Value)) : null,
            message.DocumentType,
            message.ReceiverNumber,
            message.SenderNumber,
            message.CreatedAt,
            message.BusinessReason,
            FileStorageReference = message.FileStorageReference.Path,
            message.MessageId,
            RelatedToMessageId = message.RelatedToMessageId?.Value,
        };

        await connection.ExecuteAsync(sql, parameters).ConfigureAwait(false);
    }

    public async Task<ArchivedMessageStream?> GetAsync(ArchivedMessageId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);

        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

        var fileStorageReferenceString = await connection.ExecuteScalarAsync<string>(
                $"SELECT FileStorageReference FROM dbo.[ArchivedMessages] WHERE Id = @Id",
                new { Id = id.Value.ToString() })
            .ConfigureAwait(false);

        if (fileStorageReferenceString == null)
            return null;

        var fileStorageReference = new FileStorageReference(ArchivedMessage.FileStorageCategory, fileStorageReferenceString);

        var fileStorageFile = await _fileStorageClient.DownloadAsync(fileStorageReference).ConfigureAwait(false);

        return new ArchivedMessageStream(fileStorageFile);
    }

    public async Task<MessageSearchResult> SearchAsync(GetMessagesQuery queryInput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryInput);
        var input = new QueryBuilder(_authenticatedActor.CurrentActorIdentity).BuildFrom(queryInput);
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        var archivedMessages =
            await connection.QueryAsync<MessageInfo>(
                    input.SqlStatement,
                    input.Parameters)
                .ConfigureAwait(false);
        return new MessageSearchResult(archivedMessages.OrderBy(x => x.CreatedAt).ToList().AsReadOnly());
    }
}
