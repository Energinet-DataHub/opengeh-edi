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

using System.Xml;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Exceptions;
using FluentAssertions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

public sealed class AggregatedMeasureDataRequestDsl
{
    private readonly EdiDriver _ediDriver;
    private readonly EdiDatabaseDriver _ediDatabaseDriver;
    private readonly WholesaleDriver _wholesaleDriver;

#pragma warning disable VSTHRD200 // Since this is a DSL we don't want to suffix tasks with 'Async' since it is not part of the ubiquitous language

    internal AggregatedMeasureDataRequestDsl(
        EdiDriver ediDriver,
        EdiDatabaseDriver ediDatabaseDriver,
        WholesaleDriver wholesaleDriver)
    {
        _ediDriver = ediDriver;
        _ediDatabaseDriver = ediDatabaseDriver;
        _wholesaleDriver = wholesaleDriver;
    }

    internal Task<string> AggregatedMeasureDataWithXmlPayload(XmlDocument payload)
    {
        return _ediDriver.RequestAggregatedMeasureDataXmlAsync(payload);
    }

    internal async Task<Guid> Request(CancellationToken cancellationToken)
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);
        return await _ediDriver
                .RequestAggregatedMeasureDataAsync(false, cancellationToken)
                .ConfigureAwait(false);
    }

    internal async Task ConfirmInvalidRequestIsRejected(CancellationToken cancellationToken = default)
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);
        var act = async () =>
        {
            await _ediDriver
                .RequestAggregatedMeasureDataAsync(true, cancellationToken)
                .ConfigureAwait(false);
        };

        await Assert.ThrowsAsync<BadAggregatedMeasureDataRequestException>(act).ConfigureAwait(false);
    }

    internal async Task ConfirmRequestIsInitialized(
        Guid requestMessageId,
        CancellationToken cancellationToken)
    {
        var processId = await _ediDatabaseDriver
            .GetAggregatedMeasureDataProcessIdAsync(requestMessageId, cancellationToken)
            .ConfigureAwait(false);

        processId.Should().NotBeNull();
    }

    internal async Task PublishAggregatedMeasureDataRequestAcceptedResponse(
        string gridAreaCode,
        string actorNumber,
        CancellationToken cancellationToken)
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);

        var processId = await _ediDatabaseDriver
            .CreateAggregatedMeasureDataProcessAsync(gridAreaCode, actorNumber, cancellationToken)
            .ConfigureAwait(false);

        await _wholesaleDriver.PublishAggregatedMeasureDataRequestAcceptedResponseAsync(
            processId,
            gridAreaCode,
            cancellationToken).ConfigureAwait(false);
    }

    internal async Task PublishAggregatedMeasureDataRequestRejectedResponse(
        string gridAreaCode,
        string actorNumber,
        CancellationToken cancellationToken)
    {
        await _ediDriver.EmptyQueueAsync().ConfigureAwait(false);

        var processId = await _ediDatabaseDriver
            .CreateAggregatedMeasureDataProcessAsync(gridAreaCode, actorNumber, cancellationToken)
            .ConfigureAwait(false);

        await _wholesaleDriver.PublishAggregatedMeasureDataRequestRejectedResponseAsync(processId, cancellationToken)
            .ConfigureAwait(false);
    }
}
