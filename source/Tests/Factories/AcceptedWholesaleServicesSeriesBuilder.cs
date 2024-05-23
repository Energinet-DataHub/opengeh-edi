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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class AcceptedWholesaleServicesSeriesBuilder
{
    private readonly TransactionId _transactionId = TransactionId.New();
    private readonly int _calculationResultVersion = 1;
    private readonly string _gridAreaCode = "870";
    private readonly MeteringPointType _meteringPointType = MeteringPointType.Consumption;
    private readonly SettlementMethod? _settlementMethod = SettlementMethod.NonProfiled;
    private readonly MeasurementUnit _measurementUnit = MeasurementUnit.Kwh;
    private readonly MeasurementUnit _priceMeasureUnit = MeasurementUnit.Kwh;
    private readonly Resolution _resolution = Resolution.Monthly;
    private readonly ActorNumber _energySupplierActorNumber = ActorNumber.Create("1234567894444");
    private readonly string _chargeCode = "123";
    private readonly ChargeType _chargeType = ChargeType.Fee;
    private readonly ActorNumber _chargeOwner = ActorNumber.Create("1234567897777");
    private readonly TransactionId _originalTransactionIdReference = TransactionId.New();
    private readonly List<WholesaleServicesPoint> _points = new() { new(1, 100, 100, 100, CalculatedQuantityQuality.Missing) };
    private readonly Currency _currency = Currency.DanishCrowns;
    private readonly Period _period = new(Instant.FromUtc(2023, 11, 1, 0, 0), Instant.FromUtc(2023, 12, 1, 0, 0));
    private SettlementVersion? _settlementVersion;

    public AcceptedWholesaleServicesSeriesBuilder WithSettlementVersion(SettlementVersion? settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public AcceptedWholesaleServicesSeries BuildWholesaleCalculation()
    {
        return new AcceptedWholesaleServicesSeries(
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
            PriceMeasureUnit: _priceMeasureUnit,
            Currency: _currency,
            ChargeType: _chargeType,
            Resolution: _resolution,
            MeteringPointType: _meteringPointType,
            SettlementMethod: _settlementMethod,
            OriginalTransactionIdReference: _originalTransactionIdReference);
    }
}
