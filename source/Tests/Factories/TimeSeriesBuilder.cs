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
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;
using NodaTime;
using NodaTime.Text;
using Period = Energinet.DataHub.EDI.Common.Period;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class TimeSeriesBuilder
{
    private readonly List<Point> _points = new();
    private string _messageId = Guid.NewGuid().ToString();
    private Instant _timeStamp = SystemClock.Instance.GetCurrentInstant();
    private BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private string _receiverNumber = "1234567890123";
    private MarketRole _receiverRole = MarketRole.MeteredDataResponsible;
    private string _senderNumber = "1234567890321";
    private MarketRole _senderRole = MarketRole.MeteringDataAdministrator;
    private Guid _transactionId = Guid.NewGuid();
    private string _gridAreaCode = "870";
    private MeteringPointType _meteringPointType = MeteringPointType.Consumption;
    private SettlementType? _settlementMethod = SettlementType.NonProfiled;
    private MeasurementUnit _measurementUnit = MeasurementUnit.Kwh;
    private Resolution _resolution = Resolution.QuarterHourly;
    private string? _energySupplierNumber;
    private string? _balanceResponsibleNumber;
    private string? _originalTransactionIdReference;
    private SettlementVersion? _settlementVersion;
    private Period _period = new(SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)), SystemClock.Instance.GetCurrentInstant());

    public static TimeSeriesBuilder AggregationResult()
    {
        return new TimeSeriesBuilder();
    }

    public TimeSeriesBuilder WithPoint(Point point)
    {
        _points.Add(point);

        return this;
    }

    public TimeSeriesBuilder WithBusinessReason(BusinessReason? businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);
        _businessReason = businessReason;
        return this;
    }

    public TimeSeriesBuilder WithReceiver(string receiverNumber, MarketRole marketRole)
    {
        _receiverNumber = receiverNumber;
        _receiverRole = marketRole;
        return this;
    }

    public TimeSeriesBuilder WithSender(string senderNumber, MarketRole marketRole)
    {
        _senderNumber = senderNumber;
        _senderRole = marketRole;
        return this;
    }

    public TimeSeriesBuilder WithTransactionId(Guid transactionId)
    {
        _transactionId = transactionId;
        return this;
    }

    public TimeSeriesBuilder WithGridArea(string gridAreaCode)
    {
        _gridAreaCode = gridAreaCode;
        return this;
    }

    public TimeSeriesBuilder WithMeteringPointType(MeteringPointType meteringPointType)
    {
        _meteringPointType = meteringPointType;
        return this;
    }

    public TimeSeriesBuilder WithSettlementMethod(SettlementType? settlementType)
    {
        _settlementMethod = settlementType;
        return this;
    }

    public TimeSeriesBuilder WithMeasurementUnit(MeasurementUnit measurementUnit)
    {
        _measurementUnit = measurementUnit;
        return this;
    }

    public TimeSeriesBuilder WithResolution(Resolution resolution)
    {
        _resolution = resolution;
        return this;
    }

    public TimeSeriesBuilder WithEnergySupplierNumber(string? balanceResponsibleNumber)
    {
        _energySupplierNumber = balanceResponsibleNumber;
        return this;
    }

    public TimeSeriesBuilder WithBalanceResponsibleNumber(string? balanceResponsibleNumber)
    {
        _balanceResponsibleNumber = balanceResponsibleNumber;
        return this;
    }

    public TimeSeriesBuilder WithPeriod(Instant startOfPeriod, Instant endOfPeriod)
    {
        _period = new Period(startOfPeriod, endOfPeriod);
        return this;
    }

    public TimeSeriesBuilder WithMessageId(string messageId)
    {
        _messageId = messageId;
        return this;
    }

    public TimeSeriesBuilder WithTimestamp(string timestamp)
    {
        _timeStamp = ParseTimeStamp(timestamp);
        return this;
    }

    public TimeSeriesBuilder WithOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        _originalTransactionIdReference = originalTransactionIdReference;
        return this;
    }

    public TimeSeriesBuilder WithSettlementVersion(SettlementVersion? settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public OutgoingMessageHeader BuildHeader()
    {
        return new OutgoingMessageHeader(
            _businessReason.Name,
            _senderNumber,
            _senderRole.Name,
            _receiverNumber,
            _receiverRole.Name,
            _messageId,
            _timeStamp);
    }

    public TimeSeries BuildTimeSeries()
    {
        return new TimeSeries(
            _transactionId,
            _gridAreaCode,
            _meteringPointType.Name,
            _settlementMethod?.Name,
            _measurementUnit.Name,
            _resolution.Name,
            _energySupplierNumber,
            _balanceResponsibleNumber,
            _period,
            _points,
            _originalTransactionIdReference,
            _settlementVersion?.Name);
    }

    private static Instant ParseTimeStamp(string timestamp)
    {
        return InstantPattern.General.Parse(timestamp).Value;
    }
}
