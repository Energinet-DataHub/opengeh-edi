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
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Protobuf;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories;

public static class WholesaleServicesTotalSumMessageFactory
{
    public static WholesaleServicesTotalSumMessageDto CreateMessage(
        EventId eventId,
        TotalMonthlyAmountResultProducedV1 totalMonthlyAmountResultProducedV1)
    {
        ArgumentNullException.ThrowIfNull(totalMonthlyAmountResultProducedV1);
        var message = CreateWholesaleResultSeries(totalMonthlyAmountResultProducedV1);
        var receiver = GetReceiver(totalMonthlyAmountResultProducedV1);
        return WholesaleServicesTotalSumMessageDto.Create(
            eventId,
            receiver.ActorNumber,
            receiver.ActorRole,
            BusinessReasonMapper.Map(totalMonthlyAmountResultProducedV1.CalculationType),
            message);
    }

    private static (ActorNumber ActorNumber, ActorRole ActorRole) GetReceiver(
        TotalMonthlyAmountResultProducedV1 totalMonthlyAmountResultProducedV1)
    {
        if (totalMonthlyAmountResultProducedV1.HasChargeOwnerId)
        {
            //ChargeOwner can either be grid operator or system operator.
            var chargeOwner = ActorNumber.Create(totalMonthlyAmountResultProducedV1.ChargeOwnerId);
            return (
                chargeOwner,
                GetChargeOwnerRole(chargeOwner));
        }

        return (ActorNumber.Create(totalMonthlyAmountResultProducedV1.EnergySupplierId), ActorRole.EnergySupplier);
    }

    private static WholesaleServicesTotalSumSeries CreateWholesaleResultSeries(TotalMonthlyAmountResultProducedV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new WholesaleServicesTotalSumSeries(
            TransactionId: TransactionId.New(),
            CalculationVersion: message.CalculationResultVersion,
            GridAreaCode: message.GridAreaCode,
            EnergySupplier: ActorNumber.Create(message.EnergySupplierId),
            Period: new Period(message.PeriodStartUtc.ToInstant(), message.PeriodEndUtc.ToInstant()),
            SettlementVersion: SettlementVersionMapper.Map(message.CalculationType),
            QuantityMeasureUnit: MeasurementUnit.Kwh,
            Currency: CurrencyMapper.Map(message.Currency),
            Resolution: Resolution.Monthly,
            Amount: DecimalParser.Parse(message.Amount));
    }

    private static ActorRole GetChargeOwnerRole(ActorNumber chargeOwnerId)
    {
        return chargeOwnerId == DataHubDetails.SystemOperatorActorNumber
            ? ActorRole.SystemOperator
            : ActorRole.GridOperator;
    }
}
