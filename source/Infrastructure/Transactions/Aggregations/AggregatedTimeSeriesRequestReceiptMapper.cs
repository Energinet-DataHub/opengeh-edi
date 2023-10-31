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
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.Edi.Responses;
using MediatR;

namespace Energinet.DataHub.EDI.Infrastructure.Transactions.Aggregations;

public class AggregatedTimeSeriesRequestReceiptMapper : IInboxEventMapper
{
    public Task<INotification> MapFromAsync(string payload, Guid referenceId, CancellationToken cancellationToken)
    {
        var gridAreas = AggregatedTimeSeriesRequestReceipt.Parser.ParseJson(payload).GridAreas;
        return Task.FromResult<INotification>(new AggregatedTimeSerieRequestReceipt(
            referenceId,
            new ReadOnlyCollection<string>(gridAreas.ToList())));
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(AggregatedTimeSeriesRequestReceipt), StringComparison.OrdinalIgnoreCase);
    }

    public string ToJson(byte[] payload)
    {
        var inboxEvent = AggregatedTimeSeriesRequestReceipt.Parser.ParseFrom(
            payload);
        return inboxEvent.ToString();
    }
}
