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

    public async Task<string> SendForwardMeteredDataInEbixAsync(CancellationToken cancellationToken)
    {
        return await _ebix.SendForwardMeteredDataAsync(cancellationToken);
    }

    public async Task ConfirmRequestIsReceivedAsync(string messageId, CancellationToken cancellationToken)
    {
        var messageIdFromRegistry = await _ediDatabaseDriver
            .GetMessageIdFromMessageRegistryAsync(messageId, cancellationToken);

        messageIdFromRegistry.Should().NotBeNull("because the forward metering data process should be initialized");
    }

    public async Task<string> SendForwardMeteredDataInEbixWithAlreadyUsedMessageIdAsync(CancellationToken cancellationToken)
    {
        return await _ebix
            .SendForwardMeteredDataWithAlreadyUsedMessageIdAsync(cancellationToken);
    }

    public void ConfirmResponseContainsValidationError(string response, string errorMessage, CancellationToken none)
    {
        response.Should().BeEquivalentTo(errorMessage);
    }

    public async Task<string> SendForwardMeteredDataInCimAsync(CancellationToken cancellationToken)
    {
        return await _ediDriver.SendForwardMeteredDataAsync(cancellationToken);
    }

    public async Task PublishEnqueueBrs021ForwardMeteredData(Actor actor)
    {
        await _ediDriver.EmptyQueueAsync(messageCategory: MessageCategory.MeasureData);
        await _processManagerDriver.PublishEnqueueBrs021AcceptedForwardMeteredDataAsync(
            actor: actor,
            start: Instant.FromUtc(2024, 12, 31, 23, 00, 00),
            end: Instant.FromUtc(2025, 01, 31, 23, 00, 00),
            originalActorMessageId: Guid.NewGuid().ToString(),
            eventId: Guid.NewGuid());
    }

    public async Task<string> ConfirmResponseIsAvailable()
    {
        var timeout = TimeSpan.FromMinutes(2); // Timeout must be above 1 minute, since bundling "duration" is set to 1 minute on dev/test.
        var (peekResponse, dequeueResponse) = await _ediDriver.PeekMessageAsync(
            messageCategory: MessageCategory.MeasureData);
        var messageId = peekResponse.Headers.GetValues("MessageId").FirstOrDefault();
        var contentString = await peekResponse.Content.ReadAsStringAsync();

        messageId.Should().NotBeNull();
        contentString.Should().NotBeNull();
        contentString.Should().Contain("NotifyValidatedMeasureData_MarketDocument");

        return messageId!;
    }
}
