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
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using NodaTime;
using NodaTime.Text;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class WholesaleServicesSeriesBuilder
{
    private string _messageId = Guid.NewGuid().ToString("N");
    private Instant _timeStamp = SystemClock.Instance.GetCurrentInstant();
    private BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private ActorNumber _receiverActorNumber = ActorNumber.Create("1234567890123");
    private ActorRole _receiverActorRole = ActorRole.MeteredDataResponsible;
    private ActorNumber _senderActorNumber = ActorNumber.Create("1234567890321");
    private ActorRole _senderActorRole = ActorRole.MeteredDataAdministrator;

    private Guid _transactionId = Guid.NewGuid();
    private int _calculationResultVersion = 1;
    private string _gridAreaCode = "870";
    private MeteringPointType _meteringPointType = MeteringPointType.Consumption;
    private SettlementMethod? _settlementMethod = SettlementMethod.NonProfiled;
    private MeasurementUnit _measurementUnit = MeasurementUnit.Kwh;
    private MeasurementUnit _priceMeasureUnit = MeasurementUnit.Kwh;
    private Resolution _resolution = Resolution.Monthly;
    private ActorNumber _energySupplierActorNumber = ActorNumber.Create("1234567894444");
    private string _chargeCode = "123";
    private ChargeType _chargeType = ChargeType.Fee;
    private ActorNumber _chargeOwner = ActorNumber.Create("1234567897777");
    private string? _originalTransactionIdReference;
    private SettlementVersion? _settlementVersion;
    private List<WholesaleServicesPoint> _points = new() { new(1, 100, 100, 100, null) };

    private Currency _currency = Currency.DanishCrowns;
    private Period _period = new(Instant.FromUtc(2023, 11, 1, 0, 0), Instant.FromUtc(2023, 12, 1, 0, 0));

    public WholesaleServicesSeriesBuilder WithBusinessReason(BusinessReason? businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);
        _businessReason = businessReason;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithMessageId(string messageId)
    {
        _messageId = messageId;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithTimestamp(string timestamp)
    {
        _timeStamp = ParseTimeStamp(timestamp);
        return this;
    }

    public WholesaleServicesSeriesBuilder WithReceiver(ActorNumber receiverActorNumber, ActorRole actorRole)
    {
        _receiverActorNumber = receiverActorNumber;
        _receiverActorRole = actorRole;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithSender(ActorNumber senderActorNumber, ActorRole actorRole)
    {
        _senderActorNumber = senderActorNumber;
        _senderActorRole = actorRole;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithTransactionId(Guid transactionId)
    {
        _transactionId = transactionId;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithGridArea(string gridAreaCode)
    {
        _gridAreaCode = gridAreaCode;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithMeasurementUnit(MeasurementUnit measurementUnit)
    {
        _measurementUnit = measurementUnit;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithPriceMeasurementUnit(MeasurementUnit priceMeasurementUnit)
    {
        _priceMeasureUnit = priceMeasurementUnit;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithResolution(Resolution resolution)
    {
        _resolution = resolution;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithEnergySupplier(ActorNumber energySupplierActorNumber)
    {
        _energySupplierActorNumber = energySupplierActorNumber;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithChargeCode(string chargeCode)
    {
        _chargeCode = chargeCode;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithChargeType(ChargeType chargeType)
    {
        _chargeType = chargeType;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithChargeOwner(ActorNumber chargeOwnerActorNumber)
    {
        _chargeOwner = chargeOwnerActorNumber;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithPeriod(Instant startOfPeriod, Instant endOfPeriod)
    {
        _period = new Period(startOfPeriod, endOfPeriod);
        return this;
    }

    public WholesaleServicesSeriesBuilder WithOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        _originalTransactionIdReference = originalTransactionIdReference;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithMeteringPointType(MeteringPointType meteringPointType)
    {
        _meteringPointType = meteringPointType;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithSettlementMethod(SettlementMethod? settlementMethod)
    {
        _settlementMethod = settlementMethod;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithSettlementVersion(SettlementVersion? settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithCurrency(Currency currency)
    {
        _currency = currency;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithCalculationVersion(int version)
    {
        _calculationResultVersion = version;
        return this;
    }

    public WholesaleServicesSeriesBuilder WithPoints(Collection<WholesaleServicesPoint> points)
    {
        _points = points.ToList();
        return this;
    }

    public OutgoingMessageHeader BuildHeader()
    {
        return new OutgoingMessageHeader(
            _businessReason.Name,
            _senderActorNumber.Value,
            _senderActorRole.Code,
            _receiverActorNumber.Value,
            _receiverActorRole.Code,
            _messageId,
            _timeStamp);
    }

    public WholesaleServicesSeries BuildWholesaleCalculation()
    {
        return new WholesaleServicesSeries(
            TransactionId: _transactionId,
            CalculationVersion: _calculationResultVersion,
            GridAreaCode: _gridAreaCode,
            ChargeCode: _chargeCode,
            IsTax: false,
            Points: _points,
            EnergySupplier: _energySupplierActorNumber,
            ChargeOwner: _chargeOwner,
            Period: _period,
            SettlementVersion: _settlementVersion,
            _measurementUnit,
            null,
            PriceMeasureUnit: _priceMeasureUnit,
            Currency: _currency,
            ChargeType: _chargeType,
            Resolution: _resolution,
            MeteringPointType: _meteringPointType,
            null,
            _settlementMethod,
            _originalTransactionIdReference);
    }

    private static Instant ParseTimeStamp(string timestamp)
    {
        return InstantPattern.General.Parse(timestamp).Value;
    }
}
