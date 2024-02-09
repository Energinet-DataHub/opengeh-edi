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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleCalculations;
using NodaTime;
using NodaTime.Text;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class WholesaleCalculationsResultMessageBuilder
{
    //private readonly long _calculationResultVersion = 1;
    private string _messageId = Guid.NewGuid().ToString();
    private Instant _timeStamp = SystemClock.Instance.GetCurrentInstant();
    private BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private ActorNumber _receiverNumber = ActorNumber.Create("1234567890123");
    private ActorRole _receiverRole = ActorRole.MeteredDataResponsible;
    private ActorNumber _senderNumber = ActorNumber.Create("1234567890321");
    private ActorRole _senderRole = ActorRole.MeteredDataAdministrator;

    private Guid _transactionId = Guid.NewGuid();
    private string _gridAreaCode = "870";
    private MeteringPointType _meteringPointType = MeteringPointType.Consumption;
    private SettlementType? _settlementMethod = SettlementType.NonProfiled;
    private MeasurementUnit _measurementUnit = MeasurementUnit.Kwh;
    private Resolution _resolution = Resolution.Monthly;
    private ActorNumber _energySupplierActorNumber = ActorNumber.Create("1234567894444");
    private ActorNumber _chargeOwner = ActorNumber.Create("1234567897777");
    private string? _originalTransactionIdReference;
    private SettlementVersion? _settlementVersion;

    private Currency _currency = Currency.DanishCrowns;
    private Period _period = new(Instant.FromUtc(2023, 11, 1, 0, 0), Instant.FromUtc(2023, 12, 1, 0, 0));

    public static WholesaleCalculationsResultMessageBuilder AggregationResult()
    {
        return new WholesaleCalculationsResultMessageBuilder();
    }

    public WholesaleCalculationsResultMessageBuilder WithBusinessReason(BusinessReason? businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);
        _businessReason = businessReason;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithReceiver(ActorNumber receiverActorNumber, ActorRole actorRole)
    {
        _receiverNumber = receiverActorNumber;
        _receiverRole = actorRole;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithSender(ActorNumber senderActorNumber, ActorRole actorRole)
    {
        _senderNumber = senderActorNumber;
        _senderRole = actorRole;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithTransactionId(Guid transactionId)
    {
        _transactionId = transactionId;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithGridArea(string gridAreaCode)
    {
        _gridAreaCode = gridAreaCode;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithMeteringPointType(MeteringPointType meteringPointType)
    {
        _meteringPointType = meteringPointType;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithSettlementMethod(SettlementType? settlementType)
    {
        _settlementMethod = settlementType;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithMeasurementUnit(MeasurementUnit measurementUnit)
    {
        _measurementUnit = measurementUnit;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithResolution(Resolution resolution)
    {
        _resolution = resolution;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithEnergySupplierNumber(ActorNumber energySupplierActorNumber)
    {
        _energySupplierActorNumber = energySupplierActorNumber;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithChargeOwner(ActorNumber chargeOwnerActorNumber)
    {
        _chargeOwner = chargeOwnerActorNumber;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithPeriod(Instant startOfPeriod, Instant endOfPeriod)
    {
        _period = new Period(startOfPeriod, endOfPeriod);
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithMessageId(string messageId)
    {
        _messageId = messageId;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithTimestamp(string timestamp)
    {
        _timeStamp = ParseTimeStamp(timestamp);
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        _originalTransactionIdReference = originalTransactionIdReference;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithSettlementVersion(SettlementVersion? settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public WholesaleCalculationsResultMessageBuilder WithCurrency(Currency currency)
    {
        _currency = currency;
        return this;
    }

    public OutgoingMessageHeader BuildHeader()
    {
        return new OutgoingMessageHeader(
            _businessReason.Name,
            _senderNumber.Value,
            _senderRole.Code,
            _receiverNumber.Value,
            _receiverRole.Code,
            _messageId,
            _timeStamp);
    }

    public WholesaleCalculationSeries BuildWholesaleCalculation()
    {
        return new WholesaleCalculationSeries(
            GridAreaCode: _gridAreaCode,
            ChargeCode: "123",
            IsTax: false,
            Quantity: 100,
            EnergySupplier: _energySupplierActorNumber,
            ChargeOwner: _chargeOwner,
            Period: _period,
            BusinessReason: _businessReason,
            SettlementVersion: _settlementVersion,
            QuantityUnit: _measurementUnit,
            Currency: _currency,
            ChargeType: ChargeType.Fee);
    }

    private static Instant ParseTimeStamp(string timestamp)
    {
        return InstantPattern.General.Parse(timestamp).Value;
    }
}
