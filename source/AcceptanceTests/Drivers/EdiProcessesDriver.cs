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
using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

internal sealed class EdiProcessesDriver
{
    private readonly string _connectionString;

    internal EdiProcessesDriver(string connectionString)
    {
        _connectionString = connectionString;
    }

    internal async Task<Guid> CreateWholesaleServiceProcessAsync(
        string requestedGridAreaCode,
        string chargeOwnerNumber,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand();
        var processId = Guid.NewGuid();

        command.CommandText = @"INSERT INTO [WholesaleServicesProcesses]
            (ProcessId, BusinessTransactionId, StartOfPeriod, EndOfPeriod, RequestedGridArea, ChargeOwner, Resolution, EnergySupplierId, BusinessReason, RequestedByActorNumber, RequestedByActorRole, OriginalActorNumber, OriginalActorRole, State, SettlementVersion, InitiatedByMessageId, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt)
            VALUES
            (@ProcessId, @BusinessTransactionId, '2022-06-17T22:00:00Z', '2022-07-22T22:00:00Z', @RequestedGridArea, @ChargeOwnerNumber, 'P1M', 5790000000002, 'D05', @ChargeOwnerNumber, 'EZ', @ChargeOwnerNumber, 'EZ', 'Sent', NULL, '318dcf73-4b3b-4b8a-ad47-64743dd77e66', 'Acceptance Tests', @CreatedAt, NULL, NULL);";
        command.Parameters.AddWithValue("@ProcessId", processId);
        command.Parameters.AddWithValue("@BusinessTransactionId", Guid.NewGuid());
        command.Parameters.AddWithValue("@RequestedGridArea", requestedGridAreaCode);
        command.Parameters.AddWithValue("@ChargeOwnerNumber", chargeOwnerNumber);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        command.Connection = connection;

        await command.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return processId;
    }

    internal async Task<Guid> CreateAggregatedMeasureDataProcessAsync(
        string requestedGridAreaCode,
        string actorNumber,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand();
        var processId = Guid.NewGuid();

        command.CommandText = @"INSERT INTO [AggregatedMeasureDataProcesses]
            (ProcessId, BusinessTransactionId, MeteringPointType, SettlementMethod, StartOfPeriod, EndOfPeriod, RequestedGridArea, EnergySupplierId, BalanceResponsibleId, RequestedByActorId, BusinessReason, RequestedByActorRoleCode, State, SettlementVersion, InitiatedByMessageId, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt)
            VALUES
            (@ProcessId, @BusinessTransactionId, 'E17', 'D01', '2024-04-22T22:00:00Z', '2024-04-23T22:00:00Z', @RequestedGridArea, @EnergySupplierId, null, @EnergySupplierId, 'D04', 'DDQ', 'Sent', NULL, '9e831318-f12c-48b0-9151-c9c6e73081dc', 'Acceptance Tests', @CreatedAt, NULL, NULL);";
        command.Parameters.AddWithValue("@ProcessId", processId);
        command.Parameters.AddWithValue("@BusinessTransactionId", Guid.NewGuid());
        command.Parameters.AddWithValue("@RequestedGridArea", requestedGridAreaCode);
        command.Parameters.AddWithValue("@EnergySupplierId", actorNumber);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        command.Connection = connection;

        await command.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
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
}
