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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Protobuf;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories;

public sealed class WholesaleServicesMessageFactory
{
    private readonly IMasterDataClient _masterDataClient;

    public WholesaleServicesMessageFactory(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    public async Task<WholesaleServicesMessageDto> CreateMessageAsync(
        EventId eventId,
        MonthlyAmountPerChargeResultProducedV1 monthlyAmountPerChargeResultProducedV1)
    {
        ArgumentNullException.ThrowIfNull(monthlyAmountPerChargeResultProducedV1);

        var message = CreateWholesaleResultSeries(monthlyAmountPerChargeResultProducedV1);

        var chargeOwner = await GetChargeOwnerReceiverAsync(
                monthlyAmountPerChargeResultProducedV1.GridAreaCode,
                monthlyAmountPerChargeResultProducedV1.ChargeOwnerId,
                monthlyAmountPerChargeResultProducedV1.IsTax)
            .ConfigureAwait(false);

        return WholesaleServicesMessageDto.Create(
            eventId,
            message.EnergySupplier,
            ActorRole.EnergySupplier,
            chargeOwner,
            BusinessReasonMapper.Map(monthlyAmountPerChargeResultProducedV1.CalculationType).Name,
            message);
    }

    public async Task<WholesaleServicesMessageDto> CreateMessageAsync(
        EventId eventId,
        AmountPerChargeResultProducedV1 amountPerChargeResultProducedV1)
    {
        ArgumentNullException.ThrowIfNull(amountPerChargeResultProducedV1);

        var message = CreateWholesaleResultSeries(amountPerChargeResultProducedV1);

        var chargeOwner = await GetChargeOwnerReceiverAsync(
                amountPerChargeResultProducedV1.GridAreaCode,
                amountPerChargeResultProducedV1.ChargeOwnerId,
                amountPerChargeResultProducedV1.IsTax)
            .ConfigureAwait(false);

        return WholesaleServicesMessageDto.Create(
            eventId,
            receiverNumber: message.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            chargeOwnerId: chargeOwner,
            businessReason: BusinessReasonMapper.Map(amountPerChargeResultProducedV1.CalculationType).Name,
            wholesaleSeries: message);
    }

    private static IReadOnlyCollection<WholesaleServicesPoint> PointsBasedOnChargeType(AmountPerChargeResultProducedV1 message)
    {
        return PointsMapper.Map(message.TimeSeriesPoints, message.ChargeType);
    }

    private WholesaleServicesSeries CreateWholesaleResultSeries(
        MonthlyAmountPerChargeResultProducedV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new WholesaleServicesSeries(
            TransactionId: TransactionId.New(),
            CalculationVersion: message.CalculationResultVersion,
            GridAreaCode: message.GridAreaCode,
            ChargeCode: message.ChargeCode,
            IsTax: message.IsTax,
            Points: new[]
            {
                new WholesaleServicesPoint(
                    1,
                    null,
                    null,
                    message.Amount != null ? DecimalParser.Parse(message.Amount) : null,
                    message.Amount != null ? CalculatedQuantityQuality.Calculated : CalculatedQuantityQuality.Missing),
            },
            EnergySupplier: ActorNumber.Create(message.EnergySupplierId),
            ActorNumber.Create(message.ChargeOwnerId),
            Period: new Period(message.PeriodStartUtc.ToInstant(), message.PeriodEndUtc.ToInstant()),
            SettlementVersion: SettlementVersionMapper.Map(message.CalculationType),
            MeasurementUnitMapper.Map(message.QuantityUnit),
            null,
            PriceMeasureUnit: MeasurementUnit.Kwh,
            Currency: CurrencyMapper.Map(message.Currency),
            ChargeType: ChargeTypeMapper.Map(message.ChargeType),
            Resolution: Resolution.Monthly,
            null,
            null,
            null);
    }

    private WholesaleServicesSeries CreateWholesaleResultSeries(
        AmountPerChargeResultProducedV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new WholesaleServicesSeries(
            TransactionId: TransactionId.New(),
            CalculationVersion: message.CalculationResultVersion,
            GridAreaCode: message.GridAreaCode,
            ChargeCode: message.ChargeCode,
            IsTax: message.IsTax,
            Points: PointsBasedOnChargeType(message),
            EnergySupplier: ActorNumber.Create(message.EnergySupplierId),
            ActorNumber.Create(message.ChargeOwnerId),
            Period: new Period(message.PeriodStartUtc.ToInstant(), message.PeriodEndUtc.ToInstant()),
            SettlementVersion: SettlementVersionMapper.Map(message.CalculationType),
            MeasurementUnitMapper.Map(message.QuantityUnit),
            null,
            PriceMeasureUnit: MeasurementUnit.Kwh,
            Currency: CurrencyMapper.Map(message.Currency),
            ChargeType: ChargeTypeMapper.Map(message.ChargeType),
            Resolution: ResolutionMapper.Map(message.Resolution),
            MeteringPointType: MeteringPointTypeMapper.Map(message.MeteringPointType),
            null,
            SettlementMethod: SettlementMethodMapper.Map(message.SettlementMethod));
    }

    private async Task<ActorNumber> GetChargeOwnerReceiverAsync(string gridAreaCode, string chargeOwnerId, bool isTax)
    {
        return isTax
            ? await _masterDataClient
                .GetGridOwnerForGridAreaCodeAsync(gridAreaCode, CancellationToken.None)
                .ConfigureAwait(false)
            : ActorNumber.Create(chargeOwnerId);
    }
}
