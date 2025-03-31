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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.Ebix;
using FluentAssertions;
using NodaTime;

namespace Energinet.DataHub.EDI.SubsystemTests.Dsl;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Dsl shouldn't contain technical terms")]
internal sealed class ForwardMeteredDataDsl(
    EbixDriver ebix,
    EdiDriver ediDriver,
    EdiDatabaseDriver ediDatabaseDriver,
    ProcessManagerDriver processManagerDriver)
{
    private readonly EbixDriver _ebix = ebix;
    private readonly EdiDriver _ediDriver = ediDriver;
    private readonly EdiDatabaseDriver _ediDatabaseDriver = ediDatabaseDriver;
    private readonly ProcessManagerDriver _processManagerDriver = processManagerDriver;

    public async Task<string> SendMeteredDataForMeteringPointInEbixAsync(CancellationToken cancellationToken)
    {
        return await _ebix.SendMeteredDataForMeteringPointAsync(cancellationToken);
    }

    public async Task ConfirmRequestIsReceivedAsync(string messageId, CancellationToken cancellationToken)
    {
        var processId = await _ediDatabaseDriver
            .GetMeteredDataForMeteringPointProcessIdAsync(messageId, cancellationToken)
            .ConfigureAwait(false);

        processId.Should().NotBeNull("because the metered data for metering point process should be initialized");
    }

    public async Task<string> SendMeteredDataForMeteringPointInEbixWithAlreadyUsedMessageIdAsync(CancellationToken cancellationToken)
    {
        return await _ebix
            .SendMeteredDataForMeteringPointWithAlreadyUsedMessageIdAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void ConfirmResponseContainsValidationError(string response, string errorMessage, CancellationToken none)
    {
        response.Should().BeEquivalentTo(errorMessage);
    }

    public async Task<string> SendMeteredDataForMeteringPointInCimAsync(CancellationToken cancellationToken)
    {
        return await _ediDriver.SendMeteredDataForMeteringPointAsync(cancellationToken);
    }

    public async Task PublishEnqueueBrs021ForwardMeteredData(Actor actor)
    {
        await _ediDriver.EmptyQueueAsync();
        await _processManagerDriver.PublishEnqueueBrs021AcceptedForwardMeteredDataAsync(
            actor: actor,
            start: Instant.FromUtc(2024, 12, 31, 23, 00, 00),
            end: Instant.FromUtc(2025, 01, 31, 23, 00, 00),
            originalActorMessageId: Guid.NewGuid().ToString(),
            eventId: Guid.NewGuid());
    }

    public async Task<string> ConfirmResponseIsAvailable()
    {
        // TODO: Maybe we should decrease bundling duration on
        var timeout = TimeSpan.FromMinutes(6); // Timeout must be above 5 minutes, since bundling is set to 5 minutes
        var (peekResponse, dequeueResponse) = await _ediDriver.PeekMessageAsync(timeout: timeout).ConfigureAwait(false);
        var messageId = peekResponse.Headers.GetValues("MessageId").FirstOrDefault();
        var contentString = await peekResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        messageId.Should().NotBeNull();
        contentString.Should().NotBeNull();
        contentString.Should().Contain("NotifyValidatedMeasureData_MarketDocument");

        return messageId!;
    }
}
