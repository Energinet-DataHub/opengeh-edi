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
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.B2C;
using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

[SuppressMessage(
    "Style",
    "VSTHRD200:Use \"Async\" suffix for async methods",
    Justification = "Dsl classes uses a naming convention based on the business domain")]
public class ArchivedMessageDsl
{
    private readonly B2CEdiDriver _b2CEdiDriver;
    private readonly EdiDatabaseDriver _ediDatabaseDriver;

    internal ArchivedMessageDsl(B2CEdiDriver b2CEdiDriver, EdiDatabaseDriver ediDatabaseDriver)
    {
        _b2CEdiDriver = b2CEdiDriver;
        _ediDatabaseDriver = ediDatabaseDriver;
    }

    internal async Task ConfirmMessageIsArchived(string messageId)
    {
        var archivedMessages = await _b2CEdiDriver.SearchArchivedMessagesAsync(
            new SearchArchivedMessagesCriteria(
                MessageId: messageId,
                CreatedDuringPeriod: null,
                SenderNumber: null,
                ReceiverNumber: null,
                DocumentTypes: null,
                BusinessReasons: null,
                IncludeRelatedMessages: false))
            .ConfigureAwait(false);

        archivedMessages.Should().NotBeNull();
        var archivedMessage = archivedMessages.Single();
        Assert.NotNull(archivedMessage.Id);
        Assert.NotNull(archivedMessage.MessageId);
        Assert.NotNull(archivedMessage.DocumentType);
        Assert.NotNull(archivedMessage.SenderNumber);
        Assert.NotNull(archivedMessage.ReceiverNumber);
        Assert.IsType<DateTime>(archivedMessage.CreatedAt);
        Assert.NotNull(archivedMessage.BusinessReason);
    }

    internal async Task<(string MessageId, Instant CreatedAfter)> PerformArchivedMessageSearch()
    {
        var unknownMessageId = Guid.NewGuid().ToString();
        var outboxCreatedAfter = SystemClock.Instance.GetCurrentInstant();
        await _b2CEdiDriver.SearchArchivedMessagesAsync(
            new SearchArchivedMessagesCriteria(
                MessageId: unknownMessageId,
                CreatedDuringPeriod: null,
                SenderNumber: null,
                ReceiverNumber: null,
                DocumentTypes: null,
                BusinessReasons: null,
                IncludeRelatedMessages: false)).ConfigureAwait(false);

        return (unknownMessageId, outboxCreatedAfter);
    }

    internal async Task ConfirmArchivedMessageSearchAuditLogExistsForMessageId(string messageId, Instant publishedAfter)
    {
        var (success, publishedAt, payload, failedAt, errorMessage) = await _ediDatabaseDriver
            .GetPublishedOutboxMessageAsync(
                publishedAfter: publishedAfter,
                outboxMessageType: AuditLogOutboxMessageV1.OutboxMessageType,
                payloadContains: messageId,
                cancellationToken: CancellationToken.None);

        using var assertionScope = new AssertionScope();

        success.Should().BeTrue();

        publishedAt.Should()
            .NotBeNull()
            .And.BeAfter(publishedAfter.ToDateTimeUtc());

        payload.Should()
            .NotBeNullOrWhiteSpace()
            .And.Match($"*\"MessageId\":\"{messageId}\"*")
            .And.Match($"*\"Activity\":\"ArchivedMessagesSearch\"*")
            .And.Match($"*\"SystemId\":\"688b2dca-7231-490f-a731-d7869d33fe5e\"*")
            .And.Match($"*\"ActorNumber\":\"{AcceptanceTestFixture.B2CActorNumber}\"*")
            .And.Match($"*\"Permissions\":\"*actors:manage*\"*");

        failedAt.Should().BeNull();
        errorMessage.Should().BeNull();
    }
}
