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
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Application.OutgoingMessages.Dequeue;
using Application.OutgoingMessages.Peek;
using Dapper;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.Peek;
using Domain.SeedWork;
using Infrastructure.Configuration.DataAccess;
using Microsoft.Data.SqlClient;

namespace Infrastructure.OutgoingMessages.Peek;

public class BundledMessages : IBundledMessages
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly B2BContext _context;

    public BundledMessages(IDatabaseConnectionFactory connectionFactory, B2BContext context)
    {
        _connectionFactory = connectionFactory;
        _context = context;
    }

    public Task AddAsync(BundledMessage bundledMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bundledMessage);
        return _context.BundledMessages.AddAsync(bundledMessage, cancellationToken).AsTask();
    }

    public async Task<DequeueResult> DequeueAsync(Guid messageId, CancellationToken cancellationToken)
    {
        const string deleteStmt = @"
            DELETE E FROM [dbo].[EnqueuedMessages] E JOIN
            (SELECT EnqueuedMessageId = value FROM [dbo].[BundledMessages]
            CROSS APPLY STRING_SPLIT(MessageIdsIncluded, ',') WHERE Id = @Id) AS P
            ON E.Id = P.EnqueuedMessageId;

            DELETE FROM [dbo].[BundledMessages] WHERE Id = @Id;
        ";

        using var connection =
            (SqlConnection)await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = deleteStmt;
        command.Parameters.Add(new SqlParameter("Id", SqlDbType.UniqueIdentifier) { Value = messageId });
        int result;

        try
        {
            result = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbException)
        {
            // Add exception logging
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw; // re-throw exception
        }

        return result > 0 ? new DequeueResult(true) : new DequeueResult(false);
    }

    public virtual async Task<BundledMessage?> GetAsync(
        MessageCategory category, ActorNumber receiverNumber, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(receiverNumber);

        using var connection =
            await _connectionFactory.GetConnectionAndOpenAsync(cancellationToken).ConfigureAwait(false);

        var sqlStatement =
            $"SELECT Id, ReceiverNumber, MessageCategory, MessageIdsIncluded, GeneratedDocument FROM dbo.BundledMessages WHERE ReceiverNumber = @ReceiverNumber AND MessageCategory = @MessageCategory";
        var result = await connection.QueryFirstOrDefaultAsync<BundledMessageDto>(
            sqlStatement,
            new
            {
                ReceiverNumber = receiverNumber.Value,
                MessageCategory = category.Name,
            }).ConfigureAwait(false);

        if (result is null)
        {
            return null;
        }

        return BundledMessage.Create(
                BundledMessageId.From(result.Id),
                ActorNumber.Create(result.ReceiverNumber),
                EnumerationType.FromName<MessageCategory>(result.MessageCategory),
                result.MessageIdsIncluded.Split(",").Select(messageId => Guid.Parse(messageId)).AsEnumerable(),
                new MemoryStream(result.GeneratedDocument));
    }
}

#pragma warning disable CA1819
public record BundledMessageDto(Guid Id, string ReceiverNumber, string MessageCategory, string MessageIdsIncluded, byte[] GeneratedDocument);
#pragma warning restore CA1819
