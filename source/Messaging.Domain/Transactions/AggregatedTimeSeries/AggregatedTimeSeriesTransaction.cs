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

using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Domain.SeedWork;

namespace Messaging.Domain.Transactions.AggregatedTimeSeries;

public class AggregatedTimeSeriesTransaction : Entity
{
    private readonly AggregatedTimeSeriesResult _aggregatedTimeSeriesResult;
    private readonly List<OutgoingMessage> _messages = new();

    public AggregatedTimeSeriesTransaction(string id, AggregatedTimeSeriesResult aggregatedTimeSeriesResult)
    {
        _aggregatedTimeSeriesResult = aggregatedTimeSeriesResult;
        Id = id;
        CreateResultMessages();
    }

    #pragma warning disable CS8618 // EF core need this private constructor
    private AggregatedTimeSeriesTransaction()
    {
    }
    #pragma warning restore

    public string Id { get; }

    private void CreateResultMessages()
    {
        foreach (var result in _aggregatedTimeSeriesResult.Series)
        {
            _messages.Add(AggregatedTimeSeriesMessage.Create(result.GridOperatorId, MarketRole.GridOperator, Id, ProcessType.BalanceFixing, result));
        }
    }
}
