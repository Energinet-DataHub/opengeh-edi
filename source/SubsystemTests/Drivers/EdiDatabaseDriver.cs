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
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Microsoft.Data.SqlClient;
using NodaTime;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

internal sealed class EdiDatabaseDriver
{
    private readonly string _connectionString;

    internal EdiDatabaseDriver(string connectionString)
    {
        _connectionString = connectionString;
    }

    internal async Task<string?> GetMessageIdFromMessageRegistryAsync(string initiatedByMessageId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand();

        command.CommandText = @"
            SELECT [MessageId] FROM [MessageRegistry] WHERE [MessageId] = @InitiatedByMessageId";
        command.Parameters.AddWithValue("@InitiatedByMessageId", initiatedByMessageId);

        return await GetMessageIdAsync(command, cancellationToken);
    }

    /// <summary>
    /// Delete outgoing messages for a calculation, since if outgoing messages already exists they won't be sent again,
    /// and the subsystem tests will fail
    /// </summary>
    internal async Task DeleteOutgoingMessagesForCalculationAsync(Guid calculationId)
    {
        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync().ConfigureAwait(false);
        await using (var deleteOutgoingMessagesCommand = new SqlCommand())
        {
            // Delete outgoing messages (OutgoingMessages table) and their bundles (Bundles table)
            // where EventId = @CalculationId in the OutgoingMessages table and outgoingmessages
            // has a foreign key to the Bundles table
            deleteOutgoingMessagesCommand.CommandText = @"
                DELETE FROM [MarketDocuments] WHERE BundleId IN (SELECT Id FROM [Bundles] WHERE Id IN (SELECT AssignedBundleId FROM [OutgoingMessages] WHERE CalculationId = @CalculationId));
                DELETE FROM [Bundles] WHERE Id IN (SELECT AssignedBundleId FROM [OutgoingMessages] WHERE CalculationId = @CalculationId);
                DELETE FROM [OutgoingMessages] WHERE CalculationId = @CalculationId;
                ";

            deleteOutgoingMessagesCommand.Parameters.AddWithValue("@CalculationId", calculationId);

            deleteOutgoingMessagesCommand.Connection = connection;
            deleteOutgoingMessagesCommand.CommandTimeout = (int)TimeSpan.FromMinutes(2).TotalSeconds;

            await deleteOutgoingMessagesCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Mark bundles from load test as dequeued from a month ago to ensure that they are cleaned up by the retention service
    /// </summary>
    internal async Task MarkBundlesFromLoadTestAsDequeuedAMonthAgoAsync(
        string relatedToMessageIdPrefix,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (var updateBundles = new SqlCommand())
        {
            updateBundles.CommandText = """
                    UPDATE Bundles
                    SET DequeuedAt = DATEADD(DAY, -1, DATEADD(MONTH, -1, GETDATE()))
                    WHERE DequeuedAt is null
                        AND EXISTS (
                            SELECT 1 FROM OutgoingMessages om
                            WHERE Bundles.Id = om.AssignedBundleId
                                AND om.RelatedToMessageId like @RelatedToMessageIdPrefix
                        )
                """;
            updateBundles.Parameters.AddWithValue("RelatedToMessageIdPrefix", relatedToMessageIdPrefix + "%");

            updateBundles.Connection = connection;
            updateBundles.CommandTimeout = (int)TimeSpan.FromMinutes(4).TotalSeconds;

            await updateBundles.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<(bool Success, string? Payload)>
        GetOutboxMessageAsync(
            Instant createdAfter,
            string outboxMessageType,
            string payloadContains,
            CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var stopWatch = Stopwatch.StartNew();

        await connection.OpenAsync(cancellationToken);
        while (stopWatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            var outboxMessage = await connection.QueryFirstOrDefaultAsync(
                    @"SELECT * FROM [Outbox]
                            WHERE [CreatedAt] >= @CreatedAfter AND
                                  [Type] = @OutboxMessageType AND
                                  [Payload] LIKE @PayloadContains
                            ORDER BY [CreatedAt] ASC",
                    new
                    {
                        CreatedAfter = createdAfter.ToDateTimeUtc(),
                        OutboxMessageType = outboxMessageType,
                        PayloadContains = $"%{payloadContains}%",
                    })
                .ConfigureAwait(false);

            if (outboxMessage != null)
            {
                return (true, outboxMessage.Payload);
            }

            await Task.Delay(500, cancellationToken)
                .ConfigureAwait(false);
        }

        await connection.CloseAsync();

        return (false, null);
    }

    internal async Task<int> CountDequeuedMessagesForCalculationAsync(Guid calculationId)
    {
        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync();

        var dequeuedMessagesCount = await connection.ExecuteScalarAsync<int>(
            sql: @"SELECT COUNT(B.[Id]) FROM [Bundles] B
                        INNER JOIN [OutgoingMessages] OM ON B.[Id] = OM.[AssignedBundleId]
                        WHERE B.DequeuedAt IS NOT NULL 
                          AND OM.[CalculationId] = @CalculationId",
            param: new { CalculationId = calculationId, });

        return dequeuedMessagesCount;
    }

    internal async Task<int> CountEnqueuedMessagesForCalculationAsync(Guid calculationId)
    {
        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync();

        var enqueuedMessagesCount = await connection.ExecuteScalarAsync<int>(
            sql: @"SELECT COUNT(B.[Id]) FROM [Bundles] B
                        INNER JOIN [OutgoingMessages] OM ON B.[Id] = OM.[AssignedBundleId]
                        WHERE OM.[CalculationId] = @CalculationId",
            param: new { CalculationId = calculationId, });

        return enqueuedMessagesCount;
    }

    internal async Task<double> CountEnqueuedNotifyValidatedMeasureDataMessagesFromLoadTestAsync()
    {
        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync();

        var enqueuedMessagesCount = await connection.ExecuteScalarAsync<int>(
            sql: """
                 SELECT COUNT(om.[Id])
                 FROM OutgoingMessages om
                 JOIN Bundles b ON om.AssignedBundleId = b.Id
                 WHERE b.[DequeuedAt] IS NULL
                 AND om.[RelatedToMessageId] like 'perf_test_%'
                 AND b.[DocumentType] = 'NotifyValidatedMeasureData'
                 """);

        return enqueuedMessagesCount;
    }

    internal async Task<List<BundleDto>> GetNotifyValidatedMeasureDataBundlesAsync(
        HashSet<Guid> ids)
    {
        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync();

        var enqueuedBundles = await connection.QueryAsync<BundleDto>(
            sql: """
                 SELECT
                    [Id],
                    [ActorMessageQueueId],
                    [RelatedToMessageId],
                    [ClosedAt]
                 FROM [Bundles]
                    WHERE ([DocumentTypeInBundle] = @DocumentTypeInBundle)
                        AND [Id] IN @Ids 
                        AND [DequeuedAt] IS NULL
                 """,
            param: new
            {
                DocumentTypeInBundle = DocumentType.NotifyValidatedMeasureData.Name,
                Ids = ids,
            });

        return enqueuedBundles.ToList();
    }

    internal async Task<List<OutgoingMessageDto>> GetNotifyValidatedMeasureDataMessagesFromLoadTestAsync(
        Guid eventId)
    {
        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync();

        var enqueuedMessages = await connection.QueryAsync<OutgoingMessageDto>(
            sql: """
                 SELECT
                    [CreatedAt],
                    [AssignedBundleId],
                    [RelatedToMessageId]
                 FROM [OutgoingMessages]
                    WHERE ([DocumentType] = @DocumentType)
                        AND [EventId] = @EventId 
                 """,
            param: new
            {
                DocumentType = DocumentType.NotifyValidatedMeasureData.Name,
                EventId = eventId,
            });

        return enqueuedMessages.ToList();
    }

    private async Task<Guid?> GetProcessIdAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var stopWatch = Stopwatch.StartNew();

        command.Connection = connection;

        while (stopWatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            await command.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is Guid processId)
                return processId;
            await command.Connection.CloseAsync().ConfigureAwait(false);
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private async Task<string?> GetMessageIdAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var stopWatch = Stopwatch.StartNew();

        command.Connection = connection;

        while (stopWatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            await command.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is string messageId)
                return messageId;
            await command.Connection.CloseAsync().ConfigureAwait(false);
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    public record BundleDto(
        Guid Id,
        Guid ActorMessageQueueId,
        string RelatedToMessageId,
        DateTime? ClosedAt);

    public record OutgoingMessageDto(
        DateTime CreatedAt,
        Guid? AssignedBundleId,
        string RelatedToMessageId);
}
