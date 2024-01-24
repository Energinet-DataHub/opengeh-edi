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
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Microsoft.Data.SqlClient;

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

        await _fileStorageClient.UploadAsync(message.FileStorageReference, message.Document).ConfigureAwait(false);

        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = @"INSERT INTO [dbo].[ArchivedMessages]
                       ([Id], [DocumentType], [ReceiverNumber], [SenderNumber], [CreatedAt], [BusinessReason], [FileStorageReference], [MessageId])
                       VALUES
                       (@Id, @DocumentType, @ReceiverNumber, @SenderNumber, @CreatedAt, @BusinessReason, @FileStorageReference, @MessageId)";

        var parameters = new
        {
            message.Id,
            message.DocumentType,
            ReceiverNumber = message.ReceiverNumber.Value,
            SenderNumber = message.SenderNumber.Value,
            message.CreatedAt,
            message.BusinessReason,
            FileStorageReference = message.FileStorageReference.Value,
            message.MessageId,
        };

        await connection.ExecuteAsync(sql, parameters).ConfigureAwait(false);
    }

    public async Task<Stream?> GetAsync(string id, CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = CreateCommand(
            $"SELECT FileStorageReference FROM dbo.[ArchivedMessages] WHERE Id = @Id",
            new List<KeyValuePair<string, object?>>
            {
                new("@Id", id),
            },
            connection);

        var fileStorageReferenceAsString = (string)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (fileStorageReferenceAsString == null)
            return null;

        var fileStorageReference = new FileStorageReference(ArchivedMessage.FileStorageCategory, fileStorageReferenceAsString);

        var stream = await _fileStorageClient.DownloadAsync(fileStorageReference).ConfigureAwait(false);

        return stream;
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

    private static SqlCommand CreateCommand(
        string sqlStatement, List<KeyValuePair<string, object?>> parameters, IDbConnection connection)
    {
        var command = connection.CreateCommand();

        command.CommandText = sqlStatement;

        foreach (var parameter in parameters)
        {
            var sqlParameter = new SqlParameter(parameter.Key, parameter.Value);
            command.Parameters.Add(sqlParameter);
        }

        return (SqlCommand)command;
    }
}
