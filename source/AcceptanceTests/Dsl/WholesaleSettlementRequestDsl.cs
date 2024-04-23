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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Exceptions;
using FluentAssertions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

public sealed class WholesaleSettlementRequestDsl
{
    private readonly EdiProcessesDriver _ediProcessesDriver;
    private readonly EdiDriver _ediDriver;
    private readonly WholesaleDriver _wholesaleDriver;

    internal WholesaleSettlementRequestDsl(
        EdiProcessesDriver ediProcessesDriver,
        EdiDriver ediDriver,
        WholesaleDriver wholesaleDriver)
    {
        _ediProcessesDriver = ediProcessesDriver;
        _ediDriver = ediDriver;
        _wholesaleDriver = wholesaleDriver;
    }

    internal async Task<Guid> InitializeWholesaleSettlementRequestAsync(
        string gridAreaCode,
        string actorNumber,
        CancellationToken cancellationToken)
    {
        return await _ediProcessesDriver
            .CreateWholesaleServiceProcessAsync(gridAreaCode, actorNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<Guid> RequestAsync(CancellationToken cancellationToken)
    {
        return await _ediDriver
            .RequestWholesaleSettlementAsync(withSyncError: false, cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task ConfirmInvalidRequestIsRejected(CancellationToken cancellationToken)
    {
        var act = async () =>
        {
            await _ediDriver
                .RequestWholesaleSettlementAsync(withSyncError: true, cancellationToken)
                .ConfigureAwait(false);
        };

        await Assert.ThrowsAsync<BadWholesaleSettlementRequestException>(act).ConfigureAwait(false);
    }

    internal async Task ConfirmRequestIsInitializedAsync(
        Guid requestMessageId,
        CancellationToken cancellationToken)
    {
        var processId = await _ediProcessesDriver
            .GetWholesaleServiceProcessIdAsync(requestMessageId, cancellationToken)
            .ConfigureAwait(false);

        processId.Should().NotBeNull();
    }

    internal async Task PublishWholesaleServicesRequestAcceptedResponseAsync(
        Guid processId,
        string gridAreaCode,
        string energySupplierNumber,
        string chargeOwnerNumber,
        CancellationToken cancellationToken)
    {
        await _wholesaleDriver.PublishWholesaleServicesRequestAcceptedResponseAsync(
            processId,
            gridAreaCode,
            energySupplierNumber,
            chargeOwnerNumber,
            cancellationToken).ConfigureAwait(false);
    }

    internal async Task PublishWholesaleServicesRequestRejectedResponseAsync(
        Guid processId,
        CancellationToken cancellationToken)
    {
        await _wholesaleDriver.PublishWholesaleServicesRequestRejectedResponseAsync(
            processId,
            cancellationToken).ConfigureAwait(false);
    }
}
