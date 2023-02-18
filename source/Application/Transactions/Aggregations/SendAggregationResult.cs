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
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.Commands.Commands;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.SeedWork;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using MediatR;

namespace Application.Transactions.Aggregations;

public class SendAggregationResult : InternalCommand
{
    [JsonConstructor]
    public SendAggregationResult(Guid id, string sendResultTo, string roleOfReceiver, string processType, Aggregation result)
    : base(id)
    {
        SendResultTo = sendResultTo;
        RoleOfReceiver = roleOfReceiver;
        ProcessType = processType;
        Result = result;
    }

    public SendAggregationResult(string sendResultTo, string roleOfReceiver, string processType, Aggregation result)
    {
        SendResultTo = sendResultTo;
        RoleOfReceiver = roleOfReceiver;
        ProcessType = processType;
        Result = result;
    }

    public SendAggregationResult(string sendResultTo, string roleOfReceiver, string processType, AggregationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        SendResultTo = sendResultTo;
        RoleOfReceiver = roleOfReceiver;
        ProcessType = processType;
        Result = new Aggregation(
            result.Points.Select(point => new Point(point.Position, point.Quantity, point.Quality, point.SampleTime)).ToList(),
            result.GridArea.Code,
            result.MeteringPointType.Name,
            result.MeasureUnitType.Name,
            result.Resolution.Name,
            new Period(result.Period.Start, result.Period.End),
            result.SettlementType?.Name,
            result.AggregatedForActor?.Value);
    }

    public string SendResultTo { get; }

    public string RoleOfReceiver { get; }

    public string ProcessType { get; }

    public Aggregation Result { get; }
}

public class SendAggregationResultHandler : IRequestHandler<SendAggregationResult, Unit>
{
    private readonly IAggregationResultForwardingRepository _repository;

    public SendAggregationResultHandler(IAggregationResultForwardingRepository repository)
    {
        _repository = repository;
    }

    public Task<Unit> Handle(SendAggregationResult request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transaction = new AggregationResultForwarding(
            TransactionId.New(),
            ActorNumber.Create(request.SendResultTo),
            EnumerationType.FromName<MarketRole>(request.RoleOfReceiver),
            EnumerationType.FromName<ProcessType>(request.ProcessType));

        transaction.SendResult(From(request.Result));

        _repository.Add(transaction);
        return Task.FromResult(Unit.Value);
    }

    private static AggregationResult From(Aggregation result)
    {
        return new AggregationResult(
            Guid.NewGuid(),
            result.Points.Select(point =>
                new Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point(point.Position, point.Quantity, point.Quality, point.SampleTime)).ToList(),
            GridArea.Create(result.GridArea),
            EnumerationType.FromName<MeteringPointType>(result.MeteringPointType),
            EnumerationType.FromName<MeasurementUnit>(result.MeasureUnitType),
            EnumerationType.FromName<Resolution>(result.Resolution),
            new Domain.Transactions.Aggregations.Period(result.Period.Start, result.Period.End),
            result.SettlementType is not null ? EnumerationType.FromName<SettlementType>(result.SettlementType) : null,
            result.AggregatedForActor is not null ? ActorNumber.Create(result.AggregatedForActor) : null);
    }
}
