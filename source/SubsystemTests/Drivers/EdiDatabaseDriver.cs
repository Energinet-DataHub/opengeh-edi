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
using Microsoft.Data.SqlClient;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

internal sealed class EdiDatabaseDriver
{
    private readonly string _connectionString;

    internal EdiDatabaseDriver(string connectionString)
    {
        _connectionString = connectionString;
    }

    internal async Task<Guid> CreateWholesaleServiceProcessAsync(
        string requestedGridAreaCode,
        string chargeOwnerNumber,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        var processId = Guid.NewGuid();

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (var createProcessCommand = new SqlCommand())
        {
            createProcessCommand.CommandText = @"INSERT INTO [WholesaleServicesProcesses]
                (ProcessId, BusinessTransactionId, StartOfPeriod, EndOfPeriod, RequestedGridArea, ChargeOwner, Resolution, EnergySupplierId, BusinessReason, RequestedByActorNumber, RequestedByActorRole, OriginalActorNumber, OriginalActorRole, State, SettlementVersion, InitiatedByMessageId, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt)
                VALUES
                (@ProcessId, @BusinessTransactionId, '2022-06-17T22:00:00Z', '2022-07-22T22:00:00Z', @RequestedGridArea, @ChargeOwnerNumber, 'P1M', 5790000000002, 'D05', @ChargeOwnerNumber, 'EZ', @ChargeOwnerNumber, 'EZ', 'Sent', NULL, '318dcf73-4b3b-4b8a-ad47-64743dd77e66', 'Subsystem Tests', @CreatedAt, NULL, NULL);";
            createProcessCommand.Parameters.AddWithValue("@ProcessId", processId);
            createProcessCommand.Parameters.AddWithValue("@BusinessTransactionId", Guid.NewGuid());
            createProcessCommand.Parameters.AddWithValue("@RequestedGridArea", requestedGridAreaCode);
            createProcessCommand.Parameters.AddWithValue("@ChargeOwnerNumber", chargeOwnerNumber);
            createProcessCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            createProcessCommand.Connection = connection;
            await createProcessCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var createProcessGridAreaCommand = new SqlCommand())
        {
            var gridAreaId = Guid.NewGuid();
            createProcessGridAreaCommand.CommandText = @"INSERT INTO [WholesaleServicesProcessGridAreas]
                (Id, WholesaleServicesProcessId, GridArea)
                VALUES
                (@Id, @ProcessId, @GridArea);";
            createProcessGridAreaCommand.Parameters.AddWithValue("@Id", gridAreaId);
            createProcessGridAreaCommand.Parameters.AddWithValue("@ProcessId", processId);
            createProcessGridAreaCommand.Parameters.AddWithValue("@GridArea", requestedGridAreaCode);

            createProcessGridAreaCommand.Connection = connection;
            await createProcessGridAreaCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        return processId;
    }

    internal async Task<Guid> CreateAggregatedMeasureDataProcessAsync(
        string requestedGridAreaCode,
        string actorNumber,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        var processId = Guid.NewGuid();

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (var createProcessCommand = new SqlCommand())
        {
            createProcessCommand.CommandText = @"INSERT INTO [AggregatedMeasureDataProcesses]
            (ProcessId, BusinessTransactionId, MeteringPointType, SettlementMethod, StartOfPeriod, EndOfPeriod, RequestedGridArea, EnergySupplierId, BalanceResponsibleId,  BusinessReason, RequestedByActorNumber, RequestedByActorRole, OriginalActorNumber, OriginalActorRole, State, SettlementVersion, InitiatedByMessageId, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt)
            VALUES
            (@ProcessId, @BusinessTransactionId, 'E17', 'D01', '2024-04-22T22:00:00Z', '2024-04-23T22:00:00Z', @RequestedGridArea, @EnergySupplierId, null, 'D04', @EnergySupplierId, 'DDQ', @EnergySupplierId, 'DDQ', 'Sent', NULL, '9e831318-f12c-48b0-9151-c9c6e73081dc', 'Subsystem Tests', @CreatedAt, NULL, NULL);";
            createProcessCommand.Parameters.AddWithValue("@ProcessId", processId);
            createProcessCommand.Parameters.AddWithValue("@BusinessTransactionId", Guid.NewGuid());
            createProcessCommand.Parameters.AddWithValue("@RequestedGridArea", requestedGridAreaCode);
            createProcessCommand.Parameters.AddWithValue("@EnergySupplierId", actorNumber);
            createProcessCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            createProcessCommand.Connection = connection;
            await createProcessCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var createProcessGridAreaCommand = new SqlCommand())
        {
            var gridAreaId = Guid.NewGuid();
            createProcessGridAreaCommand.CommandText = @"INSERT INTO [AggregatedMeasureDataProcessGridAreas]
                (Id, AggregatedMeasureDataProcessId, GridArea)
                VALUES
                (@Id, @ProcessId, @GridArea);";
            createProcessGridAreaCommand.Parameters.AddWithValue("@Id", gridAreaId);
            createProcessGridAreaCommand.Parameters.AddWithValue("@ProcessId", processId);
            createProcessGridAreaCommand.Parameters.AddWithValue("@GridArea", requestedGridAreaCode);

            createProcessGridAreaCommand.Connection = connection;
            await createProcessGridAreaCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        return processId;
    }

    internal async Task<Guid?> GetWholesaleServiceProcessIdAsync(
        Guid initiatedByMessageId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand();

        // To avoid receiving a unexpected response on the process in a later test, we mark the process as rejected.
        command.CommandText = @"
            UPDATE [WholesaleServicesProcesses] SET State = 'Rejected' WHERE InitiatedByMessageId = @InitiatedByMessageId;
            SELECT ProcessId FROM [WholesaleServicesProcesses] WHERE InitiatedByMessageId = @InitiatedByMessageId";
        command.Parameters.AddWithValue("@InitiatedByMessageId", initiatedByMessageId.ToString());
        return await GetWholesaleServiceProcessIdAsync(command, cancellationToken);
    }

    internal async Task<Guid?> GetWholesaleServiceProcessIdAsync(Instant createdAfter, string requestedByActorNumber, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand();

        // To avoid receiving a unexpected response on the process in a later test, we mark the process as rejected.
        command.CommandText = @"
            UPDATE [WholesaleServicesProcesses] SET State = 'Rejected' WHERE CreatedAt >= @CreatedAfter AND RequestedByActorNumber = @RequestedByActorNumber;
            SELECT ProcessId FROM [WholesaleServicesProcesses] WHERE CreatedAt >= @CreatedAfter AND RequestedByActorNumber = @RequestedByActorNumber";
        command.Parameters.AddWithValue("@CreatedAfter", InstantPattern.General.Format(createdAfter));
        command.Parameters.AddWithValue("@RequestedByActorNumber", requestedByActorNumber);
        return await GetWholesaleServiceProcessIdAsync(command, cancellationToken);
    }

    internal async Task<Guid?> GetAggregatedMeasureDataProcessIdAsync(
        Guid initiatedByMessageId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand();

        // To avoid receiving a unexpected response on the process in a later test, we mark the process as rejected.
        command.CommandText = @"
            UPDATE [AggregatedMeasureDataProcesses] SET State = 'Rejected' WHERE InitiatedByMessageId = @InitiatedByMessageId;
            SELECT ProcessId FROM [AggregatedMeasureDataProcesses] WHERE InitiatedByMessageId = @InitiatedByMessageId";
        command.Parameters.AddWithValue("@InitiatedByMessageId", initiatedByMessageId.ToString());

        return await GetAggregatedMeasureDataProcessIdAsync(command, cancellationToken);
    }

    internal async Task<Guid?> GetAggregatedMeasureDataProcessIdAsync(
        Instant createdAfter,
        string requestedByActorNumber,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand();
        // To avoid receiving a unexpected response on the process in a later test, we mark the process as rejected.
        command.CommandText = @"
            UPDATE [AggregatedMeasureDataProcesses] SET State = 'Rejected' WHERE CreatedAt >= @CreatedAfter AND RequestedByActorNumber = @RequestedByActorNumber;
            SELECT ProcessId FROM [AggregatedMeasureDataProcesses] WHERE CreatedAt >= @CreatedAfter AND RequestedByActorNumber = @RequestedByActorNumber";

        command.Parameters.AddWithValue("@CreatedAfter", InstantPattern.General.Format(createdAfter));
        command.Parameters.AddWithValue("@RequestedByActorNumber", requestedByActorNumber);

        return await GetAggregatedMeasureDataProcessIdAsync(command, cancellationToken);
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
            await deleteOutgoingMessagesCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
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

    private async Task<Guid?> GetAggregatedMeasureDataProcessIdAsync(SqlCommand command, CancellationToken cancellationToken)
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

    private async Task<Guid?> GetWholesaleServiceProcessIdAsync(SqlCommand command, CancellationToken cancellationToken)
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
}
