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
using Energinet.DataHub.EDI.Common.Protobuf;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.OutgoingMessages;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories;

public static class WholesaleCalculationResultMessageFactory
{
    public static WholesaleCalculationResultMessage CreateMessageForEnergySupplier(
        AmountPerChargeResultProducedV1 eventMessage)
    {
        ArgumentNullException.ThrowIfNull(eventMessage);

        var message = CreateWholesaleCalculationSeries(eventMessage);

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: message.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            processId: Guid.NewGuid(),
            businessReason: BusinessReasonMapper.Map(eventMessage.CalculationType),
            wholesaleSeries: message);
    }

    public static WholesaleCalculationResultMessage CreateMessageForChargeOwner(
        AmountPerChargeResultProducedV1 eventMessage)
    {
        ArgumentNullException.ThrowIfNull(eventMessage);

        var message = CreateWholesaleCalculationSeries(eventMessage);

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: message.ChargeOwner,
            receiverRole: GetChargeOwner(message.ChargeOwner),
            processId: Guid.NewGuid(),
            businessReason: BusinessReasonMapper.Map(eventMessage.CalculationType),
            wholesaleSeries: message);
    }

    public static WholesaleCalculationResultMessage CreateMessageForEnergySupplier(
        MonthlyAmountPerChargeResultProducedV1 eventMessage)
    {
        ArgumentNullException.ThrowIfNull(eventMessage);

        var message = CreateWholesaleCalculationSeries(eventMessage);

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: message.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            processId: Guid.NewGuid(),
            businessReason: BusinessReasonMapper.Map(eventMessage.CalculationType),
            wholesaleSeries: message);
    }

    public static WholesaleCalculationResultMessage CreateMessageForChargeOwner(
        MonthlyAmountPerChargeResultProducedV1 eventMessage)
    {
        ArgumentNullException.ThrowIfNull(eventMessage);

        var message = CreateWholesaleCalculationSeries(eventMessage);

        return WholesaleCalculationResultMessage.Create(
            receiverNumber: message.ChargeOwner,
            receiverRole: GetChargeOwner(message.ChargeOwner),
            processId: Guid.NewGuid(),
            businessReason: BusinessReasonMapper.Map(eventMessage.CalculationType),
            wholesaleSeries: message);
    }

    private static WholesaleCalculationSeries CreateWholesaleCalculationSeries(
        MonthlyAmountPerChargeResultProducedV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var wholesaleCalculationSeries = new WholesaleCalculationSeries(
            TransactionId: Guid.NewGuid(),
            CalculationVersion: message.CalculationResultVersion,
            GridAreaCode: message.GridAreaCode,
            ChargeCode: message.ChargeCode,
            IsTax: message.IsTax,
            Points: new[]
            {
                new WholesaleCalculationPoint(1, null, null, message.Amount != null ? DecimalParser.Parse(message.Amount) : null, null),
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

    private static WholesaleCalculationSeries CreateWholesaleCalculationSeries(
        AmountPerChargeResultProducedV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var wholesaleCalculationSeries = new WholesaleCalculationSeries(
            TransactionId: Guid.NewGuid(),
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

    private static ActorRole GetChargeOwner(ActorNumber chargeOwnerId)
    {
        if (chargeOwnerId == DataHubDetails.DataHubActorNumber)
        {
            return ActorRole.SystemOperator;
        }

        return ActorRole.GridOperator;
    }
}
