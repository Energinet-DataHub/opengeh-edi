﻿// Copyright 2020 Energinet DataHub A/S
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
using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

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
        using var connection = new SqlConnection(_connectionString);

        var processId = Guid.NewGuid();

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using (var createProcessCommand = new SqlCommand())
        {
            createProcessCommand.CommandText = @"INSERT INTO [WholesaleServicesProcesses]
                (ProcessId, BusinessTransactionId, StartOfPeriod, EndOfPeriod, RequestedGridArea, ChargeOwner, Resolution, EnergySupplierId, BusinessReason, RequestedByActorNumber, RequestedByActorRole, OriginalActorNumber, OriginalActorRole, State, SettlementVersion, InitiatedByMessageId, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt)
                VALUES
                (@ProcessId, @BusinessTransactionId, '2022-06-17T22:00:00Z', '2022-07-22T22:00:00Z', @RequestedGridArea, @ChargeOwnerNumber, 'P1M', 5790000000002, 'D05', @ChargeOwnerNumber, 'EZ', @ChargeOwnerNumber, 'EZ', 'Sent', NULL, '318dcf73-4b3b-4b8a-ad47-64743dd77e66', 'Acceptance Tests', @CreatedAt, NULL, NULL);";
            createProcessCommand.Parameters.AddWithValue("@ProcessId", processId);
            createProcessCommand.Parameters.AddWithValue("@BusinessTransactionId", Guid.NewGuid());
            createProcessCommand.Parameters.AddWithValue("@RequestedGridArea", requestedGridAreaCode);
            createProcessCommand.Parameters.AddWithValue("@ChargeOwnerNumber", chargeOwnerNumber);
            createProcessCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            createProcessCommand.Connection = connection;
            await createProcessCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        using (var createProcessGridAreaCommand = new SqlCommand())
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
        using var connection = new SqlConnection(_connectionString);
        var processId = Guid.NewGuid();

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using (var createProcessCommand = new SqlCommand())
        {
            createProcessCommand.CommandText = @"INSERT INTO [AggregatedMeasureDataProcesses]
            (ProcessId, BusinessTransactionId, MeteringPointType, SettlementMethod, StartOfPeriod, EndOfPeriod, RequestedGridArea, EnergySupplierId, BalanceResponsibleId,  BusinessReason, RequestedByActorNumber, RequestedByActorRole, OriginalActorNumber, OriginalActorRole, State, SettlementVersion, InitiatedByMessageId, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt)
            VALUES
            (@ProcessId, @BusinessTransactionId, 'E17', 'D01', '2024-04-22T22:00:00Z', '2024-04-23T22:00:00Z', @RequestedGridArea, @EnergySupplierId, null, 'D04', @EnergySupplierId, 'DDQ', @EnergySupplierId, 'DDQ', 'Sent', NULL, '9e831318-f12c-48b0-9151-c9c6e73081dc', 'Acceptance Tests', @CreatedAt, NULL, NULL);";
            createProcessCommand.Parameters.AddWithValue("@ProcessId", processId);
            createProcessCommand.Parameters.AddWithValue("@BusinessTransactionId", Guid.NewGuid());
            createProcessCommand.Parameters.AddWithValue("@RequestedGridArea", requestedGridAreaCode);
            createProcessCommand.Parameters.AddWithValue("@EnergySupplierId", actorNumber);
            createProcessCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            createProcessCommand.Connection = connection;
            await createProcessCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        using (var createProcessGridAreaCommand = new SqlCommand())
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
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand();

        var stopWatch = Stopwatch.StartNew();

        // To avoid receiving a unexpected response on the process in a later test, we mark the process as rejected.
        command.CommandText = @"
            UPDATE [WholesaleServicesProcesses] SET State = 'Rejected' WHERE InitiatedByMessageId = @InitiatedByMessageId;
            SELECT ProcessId FROM [WholesaleServicesProcesses] WHERE InitiatedByMessageId = @InitiatedByMessageId";
        command.Parameters.AddWithValue("@InitiatedByMessageId", initiatedByMessageId.ToString());
        command.Connection = connection;

        while (stopWatch.ElapsedMilliseconds < 30000)
        {
            await command.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is Guid messageId)
                return messageId;
            await command.Connection.CloseAsync().ConfigureAwait(false);
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    internal async Task<Guid?> GetAggregatedMeasureDataProcessIdAsync(
        Guid initiatedByMessageId,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand();

        var stopWatch = Stopwatch.StartNew();

        // To avoid receiving a unexpected response on the process in a later test, we mark the process as rejected.
        command.CommandText = @"
            UPDATE [AggregatedMeasureDataProcesses] SET State = 'Rejected' WHERE InitiatedByMessageId = @InitiatedByMessageId;
            SELECT ProcessId FROM [AggregatedMeasureDataProcesses] WHERE InitiatedByMessageId = @InitiatedByMessageId";
        command.Parameters.AddWithValue("@InitiatedByMessageId", initiatedByMessageId.ToString());
        command.Connection = connection;

        while (stopWatch.ElapsedMilliseconds < 30000)
        {
            await command.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is Guid messageId)
                return messageId;
            await command.Connection.CloseAsync().ConfigureAwait(false);
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    /// <summary>
    /// Delete outgoing messages for a calculation, since if outgoing messages already exists they won't be sent again,
    /// and the acceptance tests will fail
    /// </summary>
    internal async Task DeleteOutgoingMessagesForCalculationAsync(Guid calculationId)
    {
        using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync().ConfigureAwait(false);
        using (var deleteOutgoingMessagesCommand = new SqlCommand())
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
}
