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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Transactions.Aggregations;

public class AggregatedTimeSeriesRequestRejectedMapper : IInboxEventMapper
{
    public Task<INotification> MapFromAsync(string payload, Guid referenceId, CancellationToken cancellationToken)
    {
        var inboxEvent =
            AggregatedTimeSeriesRequestRejected.Parser.ParseJson(payload);
        return Task.FromResult<INotification>(
            new AggregatedTimeSeriesRequestWasRejected(
                referenceId,
                MapRejectReasons(inboxEvent.RejectReasons)));
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(AggregatedTimeSeriesRequestRejected), StringComparison.OrdinalIgnoreCase);
    }

    public string ToJson(byte[] payload)
    {
        var inboxEvent = AggregatedTimeSeriesRequestRejected.Parser.ParseFrom(
            payload);
        return inboxEvent.ToString();
    }

    private static IReadOnlyList<Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.RejectReason> MapRejectReasons(RepeatedField<RejectReason> rejectReasons)
    {
        return rejectReasons.Select(reason => new Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.RejectReason(reason.ErrorCode, reason.ErrorMessage)).ToList();
    }
}
