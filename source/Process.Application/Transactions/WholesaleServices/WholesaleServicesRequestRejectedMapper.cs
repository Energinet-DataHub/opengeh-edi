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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Notifications;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;

public sealed class WholesaleServicesRequestRejectedMapper : IInboxEventMapper
{
#pragma warning disable CA1822
    public Task<INotification> MapFromAsync(byte[] payload, EventId eventId, Guid referenceId, CancellationToken cancellationToken)
#pragma warning restore CA1822
    {
        var inboxEvent =
            WholesaleServicesRequestRejected.Parser.ParseFrom(payload);

        return Task.FromResult<INotification>(
            new WholesaleServicesRequestWasRejected(
                eventId,
                referenceId,
                MapRejectReasons(inboxEvent.RejectReasons)));
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(WholesaleServicesRequestRejected), StringComparison.OrdinalIgnoreCase);
    }

    private static ReadOnlyCollection<RejectReasonDto> MapRejectReasons(
        RepeatedField<Edi.Responses.RejectReason> rejectReasons)
    {
        return rejectReasons
            .Select(
                reason => new RejectReasonDto(
                    reason.ErrorCode,
                    reason.ErrorMessage))
            .ToList()
            .AsReadOnly();
    }
}
