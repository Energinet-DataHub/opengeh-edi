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

        var message = await CreateWholesaleResultSeriesAsync(monthlyAmountPerChargeResultProducedV1)
            .ConfigureAwait(false);

        return WholesaleServicesMessageDto.Create(
            eventId,
            message.EnergySupplier,
            ActorRole.EnergySupplier,
            message.ChargeOwner,
            BusinessReasonMapper.Map(monthlyAmountPerChargeResultProducedV1.CalculationType).Name,
            message);
    }

    public Task<WholesaleServicesMessageDto> CreateMessageAsync(
        EventId eventId,
        TotalMonthlyAmountResultProducedV1 totalMonthlyAmountResultProducedV1)
    {
        ArgumentNullException.ThrowIfNull(totalMonthlyAmountResultProducedV1);
        throw new NotImplementedException();
        // var message = await CreateWholesaleResultSeriesAsync(totalMonthlyAmountResultProducedV1)
        //     .ConfigureAwait(false);
        //
        // return WholesaleServicesMessageDto.Create(
        //     eventId,
        //     message.EnergySupplier,
        //     ActorRole.EnergySupplier,
        //     message.ChargeOwner,
        //     BusinessReasonMapper.Map(totalMonthlyAmountResultProducedV1.CalculationType).Name,
        //     message);
    }

    public async Task<WholesaleServicesMessageDto> CreateMessageAsync(
        EventId eventId,
        AmountPerChargeResultProducedV1 amountPerChargeResultProducedV1)
    {
        ArgumentNullException.ThrowIfNull(amountPerChargeResultProducedV1);

        var message = await CreateWholesaleResultSeriesAsync(amountPerChargeResultProducedV1).ConfigureAwait(false);

        return WholesaleServicesMessageDto.Create(
            eventId,
            receiverNumber: message.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            chargeOwnerId: message.ChargeOwner,
            businessReason: BusinessReasonMapper.Map(amountPerChargeResultProducedV1.CalculationType).Name,
            wholesaleSeries: message);
    }

    private async Task<WholesaleServicesSeries> CreateWholesaleResultSeriesAsync(
        MonthlyAmountPerChargeResultProducedV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var chargeOwner = await GetChargeOwnerAsync(message.GridAreaCode, message.ChargeOwnerId, message.IsTax)
            .ConfigureAwait(false);

        var wholesaleCalculationSeries = new WholesaleServicesSeries(
            TransactionId: Guid.NewGuid(),
            CalculationVersion: message.CalculationResultVersion,
            GridAreaCode: message.GridAreaCode,
            ChargeCode: message.ChargeCode,
            IsTax: message.IsTax,
            Points: new[]
            {
                new WholesaleServicesPoint(1, null, null, message.Amount != null ? DecimalParser.Parse(message.Amount) : null, null),
            },
            EnergySupplier: ActorNumber.Create(message.EnergySupplierId),
            chargeOwner,
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
        return wholesaleCalculationSeries;
    }

    private async Task<WholesaleServicesSeries> CreateWholesaleResultSeriesAsync(
        AmountPerChargeResultProducedV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var chargeOwner = await GetChargeOwnerAsync(message.GridAreaCode, message.ChargeOwnerId, message.IsTax)
            .ConfigureAwait(false);

        var wholesaleCalculationSeries = new WholesaleServicesSeries(
            TransactionId: Guid.NewGuid(),
            CalculationVersion: message.CalculationResultVersion,
            GridAreaCode: message.GridAreaCode,
            ChargeCode: message.ChargeCode,
            IsTax: message.IsTax,
            Points: PointsMapper.MapPoints(message.TimeSeriesPoints),
            EnergySupplier: ActorNumber.Create(message.EnergySupplierId),
            chargeOwner,
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

        return wholesaleCalculationSeries;
    }

    private async Task<ActorNumber> GetChargeOwnerAsync(string gridAreaCode, string chargeOwnerId, bool isTax)
    {
        return isTax
            ? await _masterDataClient
                .GetGridOwnerForGridAreaCodeAsync(gridAreaCode, CancellationToken.None)
                .ConfigureAwait(false)
            : ActorNumber.Create(chargeOwnerId);
    }
}
