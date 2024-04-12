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
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using MediatR;
using RejectReason = Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.RejectReason;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;

public class AggregatedTimeSeriesRequestRejectedMapper : IInboxEventMapper
{
    public Task<INotification> MapFromAsync(byte[] payload, string eventId, Guid referenceId, CancellationToken cancellationToken)
    {
        var inboxEvent =
            AggregatedTimeSeriesRequestRejected.Parser.ParseFrom(payload);
        return Task.FromResult<INotification>(
            new AggregatedTimeSeriesRequestWasRejected(
                eventId,
                referenceId,
                MapRejectReasons(inboxEvent.RejectReasons)));
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(AggregatedTimeSeriesRequestRejected), StringComparison.OrdinalIgnoreCase);
    }

    private static ReadOnlyCollection<RejectReason> MapRejectReasons(RepeatedField<Edi.Responses.RejectReason> rejectReasons)
    {
        return rejectReasons
            .Select(reason => new RejectReason(
                reason.ErrorCode,
                reason.ErrorMessage))
            .ToList()
            .AsReadOnly();
    }
}
