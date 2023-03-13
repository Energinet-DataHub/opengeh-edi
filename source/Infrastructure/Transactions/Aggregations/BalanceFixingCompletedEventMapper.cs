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
using System.Threading.Tasks;
using Application.Transactions.Aggregations;
using Domain.OutgoingMessages;
using Domain.Transactions;
using Infrastructure.Configuration.IntegrationEvents;
using MediatR;
using NodaTime.Serialization.Protobuf;

namespace Infrastructure.Transactions.Aggregations;

public class BalanceFixingCompletedEventMapper : IIntegrationEventMapper
{
    public Task<INotification> MapFromAsync(byte[] payload)
    {
        var integrationEvent = Energinet.DataHub.Wholesale.Contracts.Events.ProcessCompleted.Parser.ParseFrom(payload);
        return Task.FromResult((INotification)new AggregationProcessHasCompleted(
            Guid.Parse(integrationEvent.BatchId),
            GridArea.Create(integrationEvent.GridAreaCode),
            integrationEvent.PeriodStartUtc.ToInstant(),
            integrationEvent.PeriodEndUtc.ToInstant(),
            ProcessType.BalanceFixing));
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals("balancefixingcompleted", StringComparison.OrdinalIgnoreCase);
    }
}
