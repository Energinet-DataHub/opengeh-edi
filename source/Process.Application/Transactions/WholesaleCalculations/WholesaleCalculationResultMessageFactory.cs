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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleCalculations;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleCalculations;

public static class WholesaleCalculationResultMessageFactory
{
    public static WholesaleCalculationResultMessage CreateMessageForEnergySupplier(
        MonthlyAmountPerChargeResultProducedV1 message,
        ProcessId processId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(processId);

        var wholesaleCalculationSeries = CreateWholesaleCalculationSeries(message);

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: wholesaleCalculationSeries.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            processId: processId,
            businessReason: BusinessReasonMapper.Map(message.CalculationType),
            wholesaleSeries: wholesaleCalculationSeries);
    }

    public static WholesaleCalculationResultMessage CreateMessageForChargeOwner(
        MonthlyAmountPerChargeResultProducedV1 message,
        ProcessId processId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(processId);

        var wholesaleCalculationSeries = CreateWholesaleCalculationSeries(message);

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: wholesaleCalculationSeries.ChargeOwner,
            receiverRole: GetChargeOwner(wholesaleCalculationSeries.ChargeOwner),
            processId: processId,
            businessReason: BusinessReasonMapper.Map(message.CalculationType),
            wholesaleSeries: wholesaleCalculationSeries);
    }

    public static WholesaleCalculationResultMessage CreateMessageForEnergySupplier(
        AmountPerChargeResultProducedV1 message,
        ProcessId processId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(processId);

        var wholesaleCalculationSeries = CreateWholesaleCalculationSeries(message);

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: wholesaleCalculationSeries.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            processId: processId,
            businessReason: BusinessReasonMapper.Map(message.CalculationType),
            wholesaleSeries: wholesaleCalculationSeries);
    }

    public static WholesaleCalculationResultMessage CreateMessageForChargeOwner(
        AmountPerChargeResultProducedV1 message,
        ProcessId processId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(processId);

        var wholesaleCalculationSeries = CreateWholesaleCalculationSeries(message);

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: wholesaleCalculationSeries.ChargeOwner,
            receiverRole: GetChargeOwner(wholesaleCalculationSeries.ChargeOwner),
            processId: processId,
            businessReason: BusinessReasonMapper.Map(message.CalculationType),
            wholesaleSeries: wholesaleCalculationSeries);
    }

    private static ActorRole GetChargeOwner(ActorNumber chargeOwnerId)
    {
        if (chargeOwnerId == DataHubDetails.DataHubActorNumber)
        {
            return ActorRole.SystemOperator;
        }

        return ActorRole.GridOperator;
    }

    private static WholesaleCalculationSeries CreateWholesaleCalculationSeries(
        MonthlyAmountPerChargeResultProducedV1 message)
    {
        var wholesaleCalculationSeries = new WholesaleCalculationSeries(
            TransactionId: ProcessId.New().Id,
            CalculationVersion: message.CalculationResultVersion,
            GridAreaCode: message.GridAreaCode,
            ChargeCode: message.ChargeCode,
            IsTax: message.IsTax,
            Points: new[]
            {
                new Point(1, null, null, message.Amount != null ? DecimalParser.Parse(message.Amount) : null, null),
            },
            EnergySupplier: ActorNumber.Create(message.EnergySupplierId),
            ChargeOwner: ActorNumber.Create(message.ChargeOwnerId),
            Period: new Period(message.PeriodStartUtc.ToInstant(), message.PeriodEndUtc.ToInstant()),
            SettlementVersion: SettlementVersionMapper.Map(message.CalculationType),
            QuantityUnit: MeasurementUnitMapper.Map(message.QuantityUnit),
            PriceMeasureUnit: MeasurementUnit.Kwh,
            Currency: CurrencyMapper.Map(message.Currency),
            ChargeType: ChargeTypeMapper.Map(message.ChargeType),
            Resolution: Resolution.Monthly,
            null,
            null);
        return wholesaleCalculationSeries;
    }

    private static WholesaleCalculationSeries CreateWholesaleCalculationSeries(AmountPerChargeResultProducedV1 message)
    {
        var wholesaleCalculationSeries = new WholesaleCalculationSeries(
            TransactionId: ProcessId.New().Id,
            CalculationVersion: message.CalculationResultVersion,
            GridAreaCode: message.GridAreaCode,
            ChargeCode: message.ChargeCode,
            IsTax: message.IsTax,
            Points: TimeSeriesPointsMapper.MapPoints(message.TimeSeriesPoints),
            EnergySupplier: ActorNumber.Create(message.EnergySupplierId),
            ChargeOwner: ActorNumber.Create(message.ChargeOwnerId),
            Period: new Period(message.PeriodStartUtc.ToInstant(), message.PeriodEndUtc.ToInstant()),
            SettlementVersion: SettlementVersionMapper.Map(message.CalculationType),
            QuantityUnit: MeasurementUnitMapper.Map(message.QuantityUnit),
            PriceMeasureUnit: MeasurementUnit.Kwh,
            Currency: CurrencyMapper.Map(message.Currency),
            ChargeType: ChargeTypeMapper.Map(message.ChargeType),
            Resolution: Resolution.Monthly,
            MeteringPointType: MeteringPointTypeMapper.Map(message.MeteringPointType),
            SettlementType: SettlementTypeMapper.Map(message.SettlementMethod));
        return wholesaleCalculationSeries;
    }
}
