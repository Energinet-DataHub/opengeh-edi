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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using NodaTime;
using NodaTime.Text;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace OutgoingMessages.Application.Tests.Factories;

public class EnergyResultMessageTimeSeriesBuilder
{
    private readonly List<EnergyResultMessagePoint> _points = new();
    private readonly long _calculationResultVersion = 1;
    private string _messageId = Guid.NewGuid().ToString();
    private Instant _timeStamp = SystemClock.Instance.GetCurrentInstant();
    private BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private string _receiverNumber = "1234567890123";
    private ActorRole _receiverRole = ActorRole.MeteredDataResponsible;
    private string _senderNumber = "1234567890321";
    private ActorRole _senderRole = ActorRole.MeteredDataAdministrator;
    private Guid _transactionId = Guid.NewGuid();
    private string _gridAreaCode = "870";
    private MeteringPointType _meteringPointType = MeteringPointType.Consumption;
    private SettlementMethod? _settlementMethod = SettlementMethod.NonProfiled;
    private MeasurementUnit _measurementUnit = MeasurementUnit.Kwh;
    private Resolution _resolution = Resolution.QuarterHourly;
    private string? _energySupplierNumber;
    private string? _balanceResponsibleNumber;
    private SettlementVersion? _settlementVersion;
    private Period _period = new(SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)), SystemClock.Instance.GetCurrentInstant());

    public static EnergyResultMessageTimeSeriesBuilder AggregationResult()
    {
        return new EnergyResultMessageTimeSeriesBuilder();
    }

    public EnergyResultMessageTimeSeriesBuilder WithPoint(EnergyResultMessagePoint point)
    {
        _points.Add(point);

        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithBusinessReason(BusinessReason? businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);
        _businessReason = businessReason;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithReceiver(string receiverNumber, ActorRole actorRole)
    {
        _receiverNumber = receiverNumber;
        _receiverRole = actorRole;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithSender(string senderNumber, ActorRole actorRole)
    {
        _senderNumber = senderNumber;
        _senderRole = actorRole;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithTransactionId(Guid transactionId)
    {
        _transactionId = transactionId;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithGridArea(string gridAreaCode)
    {
        _gridAreaCode = gridAreaCode;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithMeteringPointType(MeteringPointType meteringPointType)
    {
        _meteringPointType = meteringPointType;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithSettlementMethod(SettlementMethod? settlementMethod)
    {
        _settlementMethod = settlementMethod;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithMeasurementUnit(MeasurementUnit measurementUnit)
    {
        _measurementUnit = measurementUnit;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithResolution(Resolution resolution)
    {
        _resolution = resolution;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithEnergySupplierNumber(string? balanceResponsibleNumber)
    {
        _energySupplierNumber = balanceResponsibleNumber;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithBalanceResponsibleNumber(string? balanceResponsibleNumber)
    {
        _balanceResponsibleNumber = balanceResponsibleNumber;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithPeriod(Instant startOfPeriod, Instant endOfPeriod)
    {
        _period = new Period(startOfPeriod, endOfPeriod);
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithMessageId(string messageId)
    {
        _messageId = messageId;
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithTimestamp(string timestamp)
    {
        _timeStamp = ParseTimeStamp(timestamp);
        return this;
    }

    public EnergyResultMessageTimeSeriesBuilder WithSettlementVersion(SettlementVersion? settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public OutgoingMessageHeader BuildHeader()
    {
        return new OutgoingMessageHeader(
            _businessReason.Name,
            _senderNumber,
            _senderRole.Code,
            _receiverNumber,
            _receiverRole.Code,
            _messageId,
            _timeStamp);
    }

    public EnergyResultMessageTimeSeries BuildTimeSeries()
    {
        return new EnergyResultMessageTimeSeries(
            _transactionId,
            _gridAreaCode,
            _meteringPointType.Name,
            null,
            _settlementMethod?.Name,
            _measurementUnit.Name,
            _resolution.Name,
            _energySupplierNumber,
            _balanceResponsibleNumber,
            _period,
            _points,
            _calculationResultVersion,
            null,
            _settlementVersion?.Name);
    }

    private static Instant ParseTimeStamp(string timestamp)
    {
        return InstantPattern.General.Parse(timestamp).Value;
    }
}
