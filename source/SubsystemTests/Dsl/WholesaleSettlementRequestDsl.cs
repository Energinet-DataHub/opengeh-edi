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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C;
using Energinet.DataHub.EDI.SubsystemTests.Exceptions;
using FluentAssertions;
using NodaTime;

namespace Energinet.DataHub.EDI.SubsystemTests.Dsl;

public sealed class WholesaleSettlementRequestDsl
{
    private readonly EdiDatabaseDriver _ediDatabaseDriver;
    private readonly EdiDriver _ediDriver;
    private readonly B2CEdiDriver _b2cEdiDriver;
    private readonly ProcessManagerDriver _processManagerDriver;

#pragma warning disable VSTHRD200 // Since this is a DSL we don't want to suffix tasks with 'Async' since it is not part of the ubiquitous language
    internal WholesaleSettlementRequestDsl(
        EdiDatabaseDriver ediDatabaseDriver,
        EdiDriver ediDriver,
        B2CEdiDriver b2cEdiDriver,
        ProcessManagerDriver processManagerDriver)
    {
        _ediDatabaseDriver = ediDatabaseDriver;
        _ediDriver = ediDriver;
        _b2cEdiDriver = b2cEdiDriver;
        _processManagerDriver = processManagerDriver;
    }

    internal async Task<Guid> Request(CancellationToken cancellationToken)
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);

        return await _ediDriver
            .RequestWholesaleSettlementAsync(withSyncError: false, cancellationToken)
            .ConfigureAwait(false);
    }

    internal Task B2CRequest(CancellationToken cancellationToken)
    {
        return _b2cEdiDriver
            .RequestWholesaleSettlementAsync(cancellationToken);
    }

    internal Task B2CRequestV2(CancellationToken cancellationToken)
    {
        return _b2cEdiDriver
            .RequestWholesaleSettlementV2Async(cancellationToken);
    }

    internal async Task ConfirmInvalidRequestIsRejected(CancellationToken cancellationToken)
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);

        var act = async () =>
        {
            await _ediDriver
                .RequestWholesaleSettlementAsync(withSyncError: true, cancellationToken)
                .ConfigureAwait(false);
        };

        await Assert.ThrowsAsync<BadWholesaleSettlementRequestException>(act).ConfigureAwait(false);
    }

    internal async Task PublishAcceptedBrs028RequestAsync(
        string gridArea,
        Actor actor)
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);

        await _processManagerDriver.PublishAcceptedBrs028RequestAsync(gridArea, actor);
    }

    internal async Task PublishRejectedBrs028RequestAsync(
        Actor actor)
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);

        await _processManagerDriver.PublishRejectedBrs028RequestAsync(actor);
    }
}
