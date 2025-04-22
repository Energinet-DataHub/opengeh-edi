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

using Dapper;
using Energinet.DataHub.EDI.ArchivedMessages.Domain;
using Energinet.DataHub.EDI.ArchivedMessages.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;

namespace Energinet.DataHub.EDI.ArchivedMessages.Infrastructure;

public class ArchivedMeteringPointMessageRepository(
    IDatabaseConnectionFactory connectionFactory,
    AuthenticatedActor authenticatedActor,
    IFileStorageClient fileStorageClient)
    : IArchivedMeteringPointMessageRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory = connectionFactory;
    private readonly AuthenticatedActor _authenticatedActor = authenticatedActor;
    private readonly IFileStorageClient _fileStorageClient = fileStorageClient;

    public async Task AddAsync(ArchivedMeteringPointMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Must upload to file storage before adding to database, to ensure the file cannot be added to the db without the file existing in file storage
        await _fileStorageClient.UploadAsync(message.FileStorageReference, message.ArchivedMessageStream.Stream)
            .ConfigureAwait(false);

        using var connection =
            await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = @"INSERT INTO [dbo].[MeteringPointArchivedMessages]
                        ([Id],
                         [EventIds],
                         [DocumentType],
                         [ReceiverNumber],
                         [ReceiverRoleCode],
                         [SenderNumber],
                         [SenderRoleCode],
                         [CreatedAt],
                         [BusinessReason],
                         [FileStorageReference],
                         [MessageId],
                         [RelatedToMessageId],
                         [MeteringPointIds])
                       VALUES
                        (@Id,
                         @EventIds,
                         @DocumentType,
                         @ReceiverNumber,
                         @ReceiverRoleCode,
                         @SenderNumber,
                         @SenderRoleCode,
                         @CreatedAt,
                         @BusinessReason,
                         @FileStorageReference,
                         @MessageId,
                         @RelatedToMessageId,
                         @MeteringPointIds)";

        var parameters = new
        {
            Id = message.Id.Value.ToString(),
            EventIds = message.EventIds.Count > 0 ? string.Join("::", message.EventIds.Select(id => id.Value)) : null,
            DocumentType = message.DocumentType.DatabaseValue,
            ReceiverNumber = message.ReceiverNumber.Value,
            ReceiverRoleCode = message.ReceiverRole.DatabaseValue,
            SenderNumber = message.SenderNumber.Value,
            SenderRoleCode = message.SenderRole.DatabaseValue,
            message.CreatedAt,
            BusinessReason = message.BusinessReason?.DatabaseValue,
            FileStorageReference = message.FileStorageReference.Path,
            message.MessageId,
            RelatedToMessageId = message.RelatedToMessageId?.Value,
            MeteringPointIds = message.MeteringPointIds.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(message.MeteringPointIds.Select(id => id.Value))
                : System.Text.Json.JsonSerializer.Serialize(new List<string>()),
        };

        await connection.ExecuteAsync(sql, parameters).ConfigureAwait(false);
    }

    public async Task<ArchivedMessageStream?> GetAsync(ArchivedMessageId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);

        var sqlStatement = $"SELECT FileStorageReference FROM dbo.[MeteringPointArchivedMessages] WHERE Id = @Id";
        DynamicParameters parameters = new();
        parameters.Add("Id", id.Value.ToString());

        if (_authenticatedActor.CurrentActorIdentity.Restriction == Restriction.Owned)
        {
            sqlStatement += $" AND ("
                            + $"( ReceiverNumber=@ActorNumber AND ReceiverRoleCode = @ActorRoleCode ) "
                            + $"OR ( SenderNumber=@ActorNumber AND SenderRoleCode = @ActorRoleCode )"
                            + $")";
            parameters.Add("ActorNumber", _authenticatedActor.CurrentActorIdentity.ActorNumber.Value);
            parameters.Add("ActorRoleCode", _authenticatedActor.CurrentActorIdentity.ActorRole.DatabaseValue);
        }

        using var connection =
            await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

        var fileStorageReferenceString = await connection.ExecuteScalarAsync<string>(
                sqlStatement,
                parameters)
            .ConfigureAwait(false);

        if (fileStorageReferenceString == null)
            return null;

        var fileStorageReference = new FileStorageReference(
            ArchivedMessage.FileStorageCategory,
            fileStorageReferenceString);

        var fileStorageFile = await _fileStorageClient.DownloadAsync(fileStorageReference, cancellationToken)
            .ConfigureAwait(false);

        return new ArchivedMessageStream(fileStorageFile);
    }

    public async Task<MessageSearchResult> SearchAsync(GetMeteringPointMessagesQuery queryInput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryInput);
        var input = new MeteredDataQueryBuilder(_authenticatedActor.CurrentActorIdentity).BuildFrom(queryInput);
        using var connection =
            await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = $@"
            {input.SqlStatement};
            {input.SqlStatementTotalCount}";

        using var multi = await connection.QueryMultipleAsync(sql, input.Parameters).ConfigureAwait(false);
        var archivedMessages = (await multi.ReadAsync<MeteringPointMessageInfo>().ConfigureAwait(false)).ToList();
        var totalAmountOfMessages = await multi.ReadSingleAsync<int>().ConfigureAwait(false);

        // When navigating backwards the list must be reversed to get the correct order.
        // Because sql use top to limit the result set and backwards is looking at the records from behind.
        if (!queryInput.Pagination.NavigationForward)
            archivedMessages.Reverse();

        return new MessageSearchResult(
            archivedMessages.Select(x => new MessageInfo(
                x.PaginationCursor,
                x.Id,
                x.MessageId,
                DocumentType.FromDatabaseValue(x.DocumentType).Name,
                x.SenderNumber,
                ActorRole.FromDatabaseValue(x.SenderRoleCode).Code,
                x.ReceiverNumber,
                ActorRole.FromDatabaseValue(x.ReceiverRoleCode).Code,
                x.CreatedAt,
                x.BusinessReason is not null ? BusinessReason.FromDatabaseValue(x.BusinessReason.Value).Name : null)).ToList().AsReadOnly(),
            totalAmountOfMessages);
    }
}
