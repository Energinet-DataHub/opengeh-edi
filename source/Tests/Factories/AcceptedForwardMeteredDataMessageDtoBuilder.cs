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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class AcceptedForwardMeteredDataMessageDtoBuilder
{
    private Resolution _resolution;
    private Instant _startTime;
    private Instant _endTime;
    private List<(int Position, decimal Quantity, Quality Quality)> _points = [];
    private EventId _eventId;
    private ExternalId _externalId;
    private Actor _receiver;
    private BusinessReason _businessReason;
    private MessageId _relatedToMessageId;
    private string _meteringPointId;
    private MeteringPointType _meteringPointType;

    public AcceptedForwardMeteredDataMessageDtoBuilder()
    {
        _resolution = Resolution.QuarterHourly;
        _startTime = Instant.FromUtc(2025, 01, 31, 23, 0);
        _endTime = _startTime.Plus(_resolution.ToDuration());
        _points.Add((1, 1.04m, Quality.Calculated));
        _eventId = EventId.From(Guid.NewGuid());
        _externalId = new ExternalId(Guid.NewGuid());
        _receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        _businessReason = BusinessReason.PeriodicMetering;
        _relatedToMessageId = MessageId.New();
        _meteringPointId = "1111111111111";
        _meteringPointType = MeteringPointType.Consumption;
    }

    public AcceptedForwardMeteredDataMessageDto Build()
    {
        return new AcceptedForwardMeteredDataMessageDto(
            eventId: _eventId,
            externalId: _externalId,
            receiver: _receiver,
            businessReason: _businessReason,
            relatedToMessageId: MessageId.New(),
            gridAreaCode: "804",
            series: new SendMeasurementsMessageSeriesDto(
                TransactionId: TransactionId.New(),
                MeteringPointId: _meteringPointId,
                MeteringPointType: _meteringPointType,
                OriginalTransactionIdReference: TransactionId.New(),
                Product: "test-product",
                MeasurementUnit: MeasurementUnit.KilowattHour,
                RegistrationDateTime: _startTime,
                Resolution: _resolution,
                Period: new Period(_startTime, _endTime),
                Measurements: _points
                    .Select(p => new MeasurementDto(p.Position, p.Quantity, p.Quality))
                    .ToList()));
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithResolution(Resolution resolution)
    {
        _resolution = resolution;
        _endTime = _startTime.Plus(_resolution.ToDuration());
        return this;
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithStartTime(Instant startTime)
    {
        _startTime = startTime;
        _endTime = _startTime.Plus(_resolution.ToDuration());
        return this;
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithEventId(EventId eventId)
    {
        _eventId = eventId;
        return this;
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithReceiver(Actor receiver)
    {
        _receiver = receiver;
        return this;
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithBusinessReason(BusinessReason businessReason)
    {
        _businessReason = businessReason;
        return this;
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithRelatedToMessageId(MessageId relatedToMessageId)
    {
        _relatedToMessageId = relatedToMessageId;
        return this;
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithExternalId(ExternalId externalId)
    {
        _externalId = externalId;
        return this;
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithMarketEvaluationPointNumber(string marketEvaluationPointNumber)
    {
        _meteringPointId = marketEvaluationPointNumber;
        return this;
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithMarketEvaluationPointType(MeteringPointType marketEvaluationPointType)
    {
        _meteringPointType = marketEvaluationPointType;
        return this;
    }

    public AcceptedForwardMeteredDataMessageDtoBuilder WithPoints(params (int Position, decimal Quantity, Quality Quality)[] points)
    {
        _points = points.ToList();
        return this;
    }
}
