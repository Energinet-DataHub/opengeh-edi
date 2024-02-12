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
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleCalculations;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf.WellKnownTypes;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleCalculations;

public class WholesaleCalculationResultMessageFactory
{
#pragma warning disable CA1822
    public WholesaleCalculationResultMessage CreateMessage(
        MonthlyAmountPerChargeResultProducedV1 message,
        ProcessId processId)
#pragma warning restore CA1822
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(processId);

        var wholesaleCalculation = CreateWholesaleCalculation(message);

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: wholesaleCalculation.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            processId: processId,
            businessReason: wholesaleCalculation.BusinessReason,
            wholesaleSeries: wholesaleCalculation);
    }

    private static WholesaleCalculationSeries CreateWholesaleCalculation(
        MonthlyAmountPerChargeResultProducedV1 monthlyAmountPerChargeResultProducedV1)
    {
        return new WholesaleCalculationSeries(
            TransactionId: ProcessId.New().Id,
            GridAreaCode: monthlyAmountPerChargeResultProducedV1.GridAreaCode,
            ChargeCode: monthlyAmountPerChargeResultProducedV1.ChargeCode,
            IsTax: monthlyAmountPerChargeResultProducedV1.IsTax,
            Quantity: MapQuantity(monthlyAmountPerChargeResultProducedV1.Amount),
            EnergySupplier: ActorNumber.Create(monthlyAmountPerChargeResultProducedV1.EnergySupplierId),
            ChargeOwner: ActorNumber.Create(monthlyAmountPerChargeResultProducedV1.ChargeOwnerId), // this is an assumption
            Period: MapPeriod(
                monthlyAmountPerChargeResultProducedV1.PeriodStartUtc,
                monthlyAmountPerChargeResultProducedV1.PeriodEndUtc),
            BusinessReason: MapBusinessReason(monthlyAmountPerChargeResultProducedV1.CalculationType),
            SettlementVersion: MapSettlementVersion(monthlyAmountPerChargeResultProducedV1.CalculationType),
            QuantityUnit: MapQuantityUnit(monthlyAmountPerChargeResultProducedV1.QuantityUnit),
            Currency: MapCurrency(monthlyAmountPerChargeResultProducedV1.Currency),
            ChargeType: MapChargeType(monthlyAmountPerChargeResultProducedV1.ChargeType));
    }

    private static decimal? MapQuantity(Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue? amount)
    {
        if (amount is null)
        {
            return null;
        }

        const decimal nanoFactor = 1_000_000_000;
        return amount.Units + (amount.Nanos / nanoFactor);
    }

    private static Period MapPeriod(Timestamp start, Timestamp end)
    {
        return new Period(start.ToInstant(), end.ToInstant());
    }

    private static BusinessReason MapBusinessReason(
        MonthlyAmountPerChargeResultProducedV1.Types.CalculationType calculationType)
    {
        return calculationType switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing => BusinessReason.WholesaleFixing,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.FirstCorrectionSettlement => BusinessReason.Correction,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.SecondCorrectionSettlement => BusinessReason.Correction,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.ThirdCorrectionSettlement => BusinessReason.Correction,
            _ => throw new ArgumentOutOfRangeException(nameof(calculationType), calculationType, null),
        };
    }

    private static SettlementVersion? MapSettlementVersion(MonthlyAmountPerChargeResultProducedV1.Types.CalculationType calculationType)
    {
        return calculationType switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing => null,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.FirstCorrectionSettlement => SettlementVersion.FirstCorrection,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.SecondCorrectionSettlement => SettlementVersion.SecondCorrection,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.ThirdCorrectionSettlement => SettlementVersion.ThirdCorrection,
            _ => throw new ArgumentOutOfRangeException(nameof(calculationType), calculationType, null),
        };
    }

    private static MeasurementUnit MapQuantityUnit(MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit quantityUnit)
    {
        return quantityUnit switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh => MeasurementUnit.Kwh,
            MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Pieces => MeasurementUnit.Pieces,
            _ => throw new InvalidOperationException($"Unknown quantity unit: {quantityUnit}"),
        };
    }

    private static Currency MapCurrency(MonthlyAmountPerChargeResultProducedV1.Types.Currency currency)
    {
        return currency switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.Currency.Dkk => Currency.DanishCrowns,
            _ => throw new InvalidOperationException($"Unknown currency: {currency}"),
        };
    }

    private static ChargeType MapChargeType(MonthlyAmountPerChargeResultProducedV1.Types.ChargeType chargeType)
    {
        return chargeType switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Fee => ChargeType.Fee,
            MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Tariff => ChargeType.Tariff,
            MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Subscription => ChargeType.Subscription,
            _ => throw new InvalidOperationException($"Unknown charge type: {chargeType}"),
        };
    }
}
