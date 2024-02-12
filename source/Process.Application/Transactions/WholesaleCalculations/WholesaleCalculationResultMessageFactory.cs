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
using System.Threading;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
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

        var wholesaleCalculationSeries = new WholesaleCalculationSeries(
            GridAreaCode: message.GridAreaCode,
            ChargeCode: message.ChargeCode,
            IsTax: message.IsTax,
            Quantity: CalculateQuantity(message.Amount),
            EnergySupplier: ActorNumber.Create(message.EnergySupplierId),
            ChargeOwner: ActorNumber.Create(message.ChargeOwnerId), // this is an assumption
            Period: new Period(message.PeriodStartUtc.ToInstant(), message.PeriodEndUtc.ToInstant()),
            BusinessReason: CalculationTypeMapper.MapCalculationType(message.CalculationType),
            SettlementVersion: MapSettlementVersion(message.CalculationType),
            QuantityUnit: MapQuantityUnit(message.QuantityUnit),
            Currency: MapCurrency(message.Currency),
            ChargeType: MapChargeType(message.ChargeType));

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: wholesaleCalculationSeries.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            processId: processId,
            businessReason: wholesaleCalculationSeries.BusinessReason,
            wholesaleSeries: wholesaleCalculationSeries);
    }

    private static decimal? CalculateQuantity(Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue? amount)
    {
        if (amount is null)
        {
            return null;
        }

        const decimal nanoFactor = 1_000_000_000;
        return amount.Units + (amount.Nanos / nanoFactor);
    }

    private static BusinessReason MapCalculationType(
        MonthlyAmountPerChargeResultProducedV1.Types.CalculationType processType) // matches the name of aggregationFactory
    {
        return processType switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing => BusinessReason.WholesaleFixing,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.FirstCorrectionSettlement => BusinessReason.Correction,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.SecondCorrectionSettlement => BusinessReason.Correction,
            MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.ThirdCorrectionSettlement => BusinessReason.Correction,
            _ => throw new ArgumentOutOfRangeException(nameof(processType), processType, null),
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
