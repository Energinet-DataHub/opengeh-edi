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
            SettlementVersion: SettlementVersionMapper.MapSettlementVersion(message.CalculationType),
            QuantityUnit: MeasurementUnitMapper.MapQuantityUnit(message.QuantityUnit),
            Currency: CurrencyMapper.MapCurrency(message.Currency),
            ChargeType: ChargeTypeMapper.MapChargeType(message.ChargeType));

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
}
