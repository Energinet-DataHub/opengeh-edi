﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;
using Currency = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Currency;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;
using SettlementMethod = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementMethod;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class ActorRequestsClient(
    IOutgoingMessagesClient outgoingMessagesClient,
    IAggregatedTimeSeriesQueries aggregatedTimeSeriesQueries,
    IWholesaleServicesQueries wholeSaleServicesQueries)
        : IActorRequestsClient
{
    private static readonly RejectedEnergyResultMessageRejectReason _noEnergyDataAvailable = new(
        ErrorMessage: "E0H",
        ErrorCode: "Ingen data tilgængelig / No data available");

    private static readonly RejectedEnergyResultMessageRejectReason _noEnergyDataForRequestedGridArea = new(
        ErrorMessage: "D46",
        ErrorCode: "Ingen data i dette netområde / No data in the requested grid area");

    private static readonly RejectedWholesaleServicesMessageRejectReason _noWholesaleDataAvailable = new(
        ErrorMessage: "E0H",
        ErrorCode: "Ingen data tilgængelig / No data available");

    private static readonly RejectedWholesaleServicesMessageRejectReason _noWholesaleDataForRequestedGridArea = new(
        ErrorMessage: "D46",
        ErrorCode: "Ingen data i dette netområde / No data in the requested grid area");

    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IAggregatedTimeSeriesQueries _aggregatedTimeSeriesQueries = aggregatedTimeSeriesQueries;
    private readonly IWholesaleServicesQueries _wholesaleServicesQueries = wholeSaleServicesQueries;

    public async Task<int> EnqueueAggregatedMeasureDataAsync(
        EventId eventId,
        Guid orchestrationInstanceId,
        MessageId originalMessageId,
        TransactionId originalTransactionId,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        ActorNumber requestedByActorNumber,
        ActorRole requestedByActorRole,
        BusinessReason businessReason,
        MeteringPointType? meteringPointType,
        SettlementMethod? settlementMethod,
        SettlementVersion? settlementVersion,
        AggregatedTimeSeriesQueryParameters aggregatedTimeSeriesQueryParameters,
        CancellationToken cancellationToken)
    {
        var calculationResults = _aggregatedTimeSeriesQueries
            .GetAsync(aggregatedTimeSeriesQueryParameters).ConfigureAwait(false);

        var enqueuedCount = 0;
        await foreach (var result in calculationResults)
        {
            var points = result.TimeSeriesPoints.OrderBy(p => p.Time)
                .Select(
                    (p, index) => new AcceptedEnergyResultMessagePoint(
                        Position: index + 1,
                        Quantity: p.Quantity,
                        QuantityQuality: GetCalculatedQuantityQualityForEnergy(p.Qualities),
                        SampleTime: p.Time.ToString()))
                .ToList();

            var acceptedEnergyResult = AcceptedEnergyResultMessageDto.Create(
                receiverNumber: requestedByActorNumber,
                receiverRole: requestedByActorRole,
                documentReceiverNumber: requestedForActorNumber,
                documentReceiverRole: requestedForActorRole,
                processId: orchestrationInstanceId,
                eventId: eventId,
                gridAreaCode: result.GridArea,
                meteringPointType: result.MeteringPointType.Name,
                settlementMethod: result.SettlementMethod?.Name,
                measureUnitType: MeasurementUnit.KilowattHour.Name, // "Unit is always Kwh for energy results" - it is hardcoded in the AggregatedTimeSeriesRequestAcceptedMessageFactory
                resolution: result.Resolution.Name,
                energySupplierNumber: aggregatedTimeSeriesQueryParameters.EnergySupplierId,
                balanceResponsibleNumber: aggregatedTimeSeriesQueryParameters.BalanceResponsibleId,
                period: new Period(result.PeriodStart, result.PeriodEnd),
                points: points,
                businessReasonName: businessReason.Name,
                calculationResultVersion: result.Version,
                originalTransactionIdReference: originalTransactionId,
                settlementVersion: result.SettlementVersion?.Name,
                relatedToMessageId: originalMessageId);

            await _outgoingMessagesClient.EnqueueAsync(acceptedEnergyResult, cancellationToken).ConfigureAwait(false);
            enqueuedCount++;
        }

        return enqueuedCount;
    }

    public async Task<int> EnqueueWholesaleServicesAsync(
        WholesaleServicesQueryParameters wholesaleServicesQueryParameters,
        ActorNumber requestedByActorNumber,
        ActorRole requestedByActorRole,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        Guid orchestrationInstanceId,
        EventId eventId,
        MessageId originalMessageId,
        TransactionId originalTransactionId,
        CancellationToken cancellationToken)
    {
        var wholesaleServicesQuery = _wholesaleServicesQueries.GetAsync(wholesaleServicesQueryParameters);

        var enqueuedCount = 0;
        await foreach (var data in wholesaleServicesQuery.ConfigureAwait(false))
        {
            var points = data.TimeSeriesPoints.OrderBy(p => p.Time)
                .Select(
                    (p, index) => new WholesaleServicesPoint(
                        Position: index + 1, // Position starts at 1, so position = index + 1
                        Quantity: p.Quantity,
                        Price: p.Price,
                        Amount: p.Amount,
                        QuantityQuality: GetCalculatedQuantityQualityForWholesaleServices(
                            quantityQualities: p.Qualities,
                            resolution: data.Resolution,
                            chargeType: data.ChargeType,
                            hasPrice: p.Price != null)))
                .ToList();

            var series = new AcceptedWholesaleServicesSeries(
                TransactionId: TransactionId.New(),
                CalculationVersion: data.Version,
                GridAreaCode: data.GridArea,
                ChargeCode: data.ChargeCode,
                IsTax: false,
                Points: points,
                EnergySupplier: ActorNumber.Create(data.EnergySupplierId),
                ChargeOwner: GetChargeOwner(data.ChargeOwnerId, data.QuantityUnit),
                Period: new Period(data.Period.Start, data.Period.End),
                SettlementVersion: data.SettlementVersion,
                QuantityMeasureUnit: GetQuantityMeasureUnit(data.QuantityUnit, data.Resolution),
                PriceMeasureUnit: GetPriceMeasureUnit(points, data.Resolution, data.ChargeType),
                Currency: GetCurrency(data.Currency),
                ChargeType: data.ChargeType,
                Resolution: data.Resolution,
                MeteringPointType: data.MeteringPointType,
                SettlementMethod: data.SettlementMethod,
                OriginalTransactionIdReference: originalTransactionId);

            var enqueueWholesaleServicesMessage = AcceptedWholesaleServicesMessageDto.Create(
                receiverNumber: requestedByActorNumber,
                receiverRole: requestedByActorRole,
                documentReceiverNumber: requestedForActorNumber,
                documentReceiverRole: requestedForActorRole,
                processId: orchestrationInstanceId,
                eventId: eventId,
                businessReason: data.BusinessReason.Name,
                chargeOwnerId: ActorNumber.TryCreate(data.ChargeOwnerId),
                relatedToMessageId: originalMessageId,
                wholesaleSeries: series);

            await _outgoingMessagesClient.EnqueueAsync(enqueueWholesaleServicesMessage, cancellationToken).ConfigureAwait(false);
            enqueuedCount++;
        }

        return enqueuedCount;
    }

    public Task EnqueueRejectAggregatedMeasureDataRequestAsync(
        RejectedEnergyResultMessageDto rejectedEnergyResultMessageDto,
        CancellationToken cancellationToken)
    {
        return _outgoingMessagesClient.EnqueueAsync(rejectedEnergyResultMessageDto, cancellationToken);
    }

    public Task EnqueueRejectWholesaleServicesRequestAsync(
        RejectedWholesaleServicesMessageDto rejectedWholesaleServicesMessageDto,
        CancellationToken cancellationToken)
    {
        return _outgoingMessagesClient.EnqueueAsync(rejectedWholesaleServicesMessageDto, cancellationToken);
    }

    public async Task EnqueueRejectAggregatedMeasureDataRequestWithNoDataAsync(
        Guid orchestrationInstanceId,
        MessageId originalMessageId,
        EventId eventId,
        TransactionId originalTransactionId,
        ActorNumber requestedByActorNumber,
        ActorRole requestedByActorRole,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        BusinessReason businessReason,
        AggregatedTimeSeriesQueryParameters aggregatedTimeSeriesQueryParameters,
        CancellationToken cancellationToken)
    {
        var hasDataInAnotherGridArea = await HasDataInAnotherGridAreaAsync(
            aggregatedTimeSeriesQueryParameters,
            requestedForActorRole).ConfigureAwait(false);

        var rejectError = hasDataInAnotherGridArea
            ? _noEnergyDataForRequestedGridArea
            : _noEnergyDataAvailable;

        var rejectedEnergyResultMessageDto = new RejectedEnergyResultMessageDto(
            requestedByActorNumber,
            orchestrationInstanceId,
            eventId,
            businessReason.Name,
            requestedByActorRole,
            originalMessageId,
            new RejectedEnergyResultMessageSerie(
                TransactionId.New(),
                [rejectError],
                originalTransactionId),
            requestedForActorNumber,
            requestedForActorRole);

        await EnqueueRejectAggregatedMeasureDataRequestAsync(
                rejectedEnergyResultMessageDto,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task EnqueueRejectWholesaleServicesRequestWithNoDataAsync(
        WholesaleServicesQueryParameters queryParameters,
        ActorNumber requestedByActorNumber,
        ActorRole requestedByActorRole,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        Guid orchestrationInstanceId,
        EventId eventId,
        MessageId originalMessageId,
        TransactionId originalTransactionId,
        BusinessReason businessReason,
        CancellationToken cancellationToken)
    {
        var hasDataInAnotherGridArea = await HasDataInAnotherGridAreaAsync(
            queryParameters,
            requestedForActorRole).ConfigureAwait(false);

        var rejectError = hasDataInAnotherGridArea
            ? _noWholesaleDataForRequestedGridArea
            : _noWholesaleDataAvailable;

        var rejectedWholesaleServicesMessageDto = new RejectedWholesaleServicesMessageDto(
            requestedByActorNumber,
            orchestrationInstanceId,
            eventId,
            businessReason.Name,
            requestedByActorRole,
            originalMessageId,
            new RejectedWholesaleServicesMessageSeries(
                TransactionId.New(),
                [rejectError],
                originalTransactionId),
            requestedForActorNumber,
            requestedForActorRole);

        await EnqueueRejectWholesaleServicesRequestAsync(
                rejectedWholesaleServicesMessageDto,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> HasDataInAnotherGridAreaAsync(
        AggregatedTimeSeriesQueryParameters queryParameters,
        ActorRole requestedByActorRole)
    {
        if (queryParameters.GridAreaCodes.Count == 0)
            return false; // If grid area codes is empty, we already retrieved any data across all grid areas

        var actorCanHaveDataInOtherGridAreas = requestedByActorRole == ActorRole.EnergySupplier
                                               || requestedByActorRole == ActorRole.BalanceResponsibleParty;

        if (actorCanHaveDataInOtherGridAreas)
        {
            var queryParametersWithoutGridArea = queryParameters with
            {
                GridAreaCodes = [],
            };

            var results = _aggregatedTimeSeriesQueries.GetAsync(
                    queryParametersWithoutGridArea)
                .ConfigureAwait(false);

            await foreach (var result in results)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> HasDataInAnotherGridAreaAsync(
        WholesaleServicesQueryParameters queryParameters,
        ActorRole requestedByActorRole)
    {
        if (queryParameters.GridAreaCodes.Count == 0)
            return false;  // If grid area codes is empty, we already retrieved any data across all grid areas

        var actorCanHaveDataInOtherGridAreas = requestedByActorRole == ActorRole.EnergySupplier
                                               || requestedByActorRole == ActorRole.SystemOperator;
        if (actorCanHaveDataInOtherGridAreas)
        {
            var queryParametersWithoutGridArea = queryParameters with
            {
                GridAreaCodes = [],
            };

            var anyResultsExists = await _wholesaleServicesQueries.AnyAsync(queryParametersWithoutGridArea).ConfigureAwait(false);

            return anyResultsExists;
        }

        return false;
    }

    private Currency GetCurrency(Interfaces.Models.CalculationResults.WholesaleResults.Currency currency)
    {
        return currency switch
        {
            Interfaces.Models.CalculationResults.WholesaleResults.Currency.DKK => Currency.DanishCrowns,
            _ => throw new ArgumentOutOfRangeException(
                nameof(currency),
                currency,
                "Unknown currency"),
        };
    }

    private ActorNumber? GetChargeOwner(string? chargeOwnerId, QuantityUnit? quantityUnit)
    {
        /*
         * The charge owner should be present on the outgoing message
         * if there is a charge owner and the message is not a total sum.
         * Note that some total sums do have a charge owner (the sums for a specific charge owner) while others do not
         * (those for an energy supplier).
         * However, no total sum has a quantity unit as this is absent from the underlying data source
         * and can thus be used to determine if the message is a total sum.
         * In other words: if the message has a quantity unit, it is not a total sum.
         */
        return chargeOwnerId is not null && quantityUnit is not null
            ? ActorNumber.Create(chargeOwnerId)
            : null;
    }

    private CalculatedQuantityQuality? GetCalculatedQuantityQualityForWholesaleServices(
        IReadOnlyCollection<QuantityQuality>? quantityQualities,
        Resolution resolution,
        ChargeType? chargeType,
        bool hasPrice)
    {
        return CalculatedQuantityQualityMapper.MapForWholesaleServices(
            quantityQualities ?? [],
            resolution,
            chargeType,
            hasPrice);
    }

    private CalculatedQuantityQuality GetCalculatedQuantityQualityForEnergy(IReadOnlyCollection<QuantityQuality> quantityQualities)
    {
        return CalculatedQuantityQualityMapper.MapForEnergy(quantityQualities);
    }

    private MeasurementUnit GetQuantityMeasureUnit(QuantityUnit? quantityUnit, Resolution resolution)
    {
        if (quantityUnit is null && resolution == Resolution.Monthly)
            return MeasurementUnit.KilowattHour;

        return quantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.KilowattHour,
            QuantityUnit.Pieces => MeasurementUnit.Pieces,
            _ => throw new ArgumentException($"Invalid quantity unit {quantityUnit} and resolution {resolution} combination"),
        };
    }

    private MeasurementUnit? GetPriceMeasureUnit(
        List<WholesaleServicesPoint> points,
        Resolution resolution,
        ChargeType? chargeType)
    {
        var isTotalSum = points.Count == 1
                         && points.First().Price is null
                         && resolution == Resolution.Monthly;

        if (isTotalSum)
            return null;

        return MeasurementUnit.TryFromChargeType(chargeType);
    }
}
