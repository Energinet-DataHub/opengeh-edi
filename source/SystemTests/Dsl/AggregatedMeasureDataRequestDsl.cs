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
using System.Net;
using Energinet.DataHub.EDI.SystemTests.Drivers;
using Energinet.DataHub.EDI.SystemTests.Exceptions;
using Energinet.DataHub.EDI.SystemTests.Models;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.SystemTests.Dsl;

public class AggregatedMeasureDataRequestDsl
{
    private readonly EdiDriver _ediDriver;

    public AggregatedMeasureDataRequestDsl(EdiDriver ediDriver)
    {
        _ediDriver = ediDriver;
    }

    internal async Task RequestAggregatedMeasureDataForAsync(Actor actor, CancellationToken cancellationToken)
    {
        await _ediDriver
            .RequestAggregatedMeasureDataAsync(actor, MessageType.RequestAggregatedMeasureData, cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task InvalidRequestAggregatedMeasureDataForAsync(Actor actor, CancellationToken cancellationToken)
    {
        await _ediDriver.RequestAggregatedMeasureDataAsync(
                actor,
                MessageType.InvalidRequestAggregatedMeasureData,
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<string> ConfirmAggregatedMeasureDataResultIsAvailableForAsync(
        Actor actor,
        CancellationToken cancellationToken)
    {
        var peekResponse = await _ediDriver.PeekUntilResponseAsync(actor, cancellationToken).ConfigureAwait(false);

        var messageId = peekResponse.Headers.GetValues("MessageId").FirstOrDefault();
        var contentString = await peekResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        messageId.Should().NotBeNull();
        contentString.Should().NotBeNull();
        contentString.Should().Contain("NotifyAggregatedMeasureData_MarketDocument");

        return messageId!;
    }

    internal async Task<string> ConfirmRejectAggregatedMeasureDataResultIsAvailableForAsync(
        Actor actor,
        CancellationToken cancellationToken)
    {
        var peekResponse = await _ediDriver.PeekUntilResponseAsync(actor, cancellationToken).ConfigureAwait(false);

        var messageId = peekResponse.Headers.GetValues("MessageId").FirstOrDefault();
        var contentString = await peekResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        messageId.Should().NotBeNull();
        contentString.Should().NotBeNull();
        contentString.Should().Contain("RejectRequestAggregatedMeasureData_MarketDocument");

        return messageId!;
    }

    internal async Task DequeueForAsync(Actor actor, string messageId, CancellationToken cancellationToken)
    {
        await _ediDriver.DequeueAsync(actor, messageId, cancellationToken).ConfigureAwait(false);
    }
}
