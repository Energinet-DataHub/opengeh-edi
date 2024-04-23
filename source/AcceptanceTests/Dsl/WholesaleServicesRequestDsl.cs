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

public sealed class WholesaleServicesRequestDsl
{
    private readonly EdiProcessesDriver _ediProcessesDriver;
    private readonly EdiDriver _ediDriver;

    internal WholesaleServicesRequestDsl(EdiProcessesDriver ediProcessesDriver, EdiDriver ediDriver)
    {
        _ediProcessesDriver = ediProcessesDriver;
        _ediDriver = ediDriver;
    }

    internal async Task<Guid> InitializeWholesaleServicesRequestAsync(
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

    internal async Task RequestWithInvalidMessageAsync(CancellationToken cancellationToken)
    {
        var act = async () =>
        {
            await _ediDriver
                .RequestWholesaleSettlementAsync(withSyncError: true, cancellationToken)
                .ConfigureAwait(false);
        };

        await Assert.ThrowsAsync<BadWholesaleSettlementRequestException>(act).ConfigureAwait(false);
    }

    internal async Task ConfirmRequestIsInitiatedAsync(
        Guid requestMessageId,
        CancellationToken cancellationToken)
    {
        var processId = await _ediProcessesDriver
            .GetWholesaleServiceProcessIdAsync(requestMessageId, cancellationToken)
            .ConfigureAwait(false);

        processId.Should().NotBeNull();
    }
}
