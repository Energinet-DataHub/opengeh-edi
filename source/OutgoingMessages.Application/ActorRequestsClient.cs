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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class ActorRequestsClient(
    IOutgoingMessagesClient outgoingMessagesClient,
    IAggregatedTimeSeriesQueries aggregatedTimeSeriesQueries,
    IWholesaleServicesQueries wholeSaleServicesQueries) : IActorRequestsClient
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IAggregatedTimeSeriesQueries _aggregatedTimeSeriesQueries = aggregatedTimeSeriesQueries;
    private readonly IWholesaleServicesQueries _wholesaleServicesQueries = wholeSaleServicesQueries;

    public async Task EnqueueAggregatedMeasureDataAsync(
        string orchestrationInstanceId,
        string originalTransactionId,
        string originalMessageId,
        ActorNumber requestedForActorNumber,
        ActorRole requestedForActorRole,
        BusinessReason businessReason,
        MeteringPointType? meteringPointType,
        SettlementMethod? settlementMethod,
        SettlementVersion? settlementVersion,
        Resolution resolution,
        AggregatedTimeSeriesQueryParameters aggregatedTimeSeriesQueryParameters)
    {
        // 3a. Query data from wholesale
        var calculationResults = await _aggregatedTimeSeriesQueries
            .GetAsync(aggregatedTimeSeriesQueryParameters).ToListAsync().ConfigureAwait(false);

        // 3b. Handle no data
        if (!calculationResults.Any())
        {
            // Send NoDataRejectMessage
        }

        foreach (var result in calculationResults)
        {
            var receiverRole = ActorRole.TryFromCode("12345");
            var documentReceiverRole = ActorRole.TryFromCode("12345");

            // Temp check for compilation.
            if (receiverRole is null || documentReceiverRole is null)
                throw new ArgumentNullException($"Temp exception | receiverRole: {receiverRole}, documentReceiverRole: {documentReceiverRole}");

            // 3c. Create AcceptedEnergyResultMessageDto - (waiting for RequestCalculatedEnergyTimeSeriesAcceptedV1 model to be finished).

            var pointList = new List<AcceptedEnergyResultMessagePoint>();

            var acceptedEnergyResult = AcceptedEnergyResultMessageDto.Create(
                receiverNumber: requestedForActorNumber,
                receiverRole: requestedForActorRole,
                documentReceiverNumber: requestedForActorNumber,
                documentReceiverRole: requestedForActorRole,
                processId: Guid.Parse(orchestrationInstanceId),
                eventId: EventId.From(originalMessageId),
                gridAreaCode: result.GridArea,
                meteringPointType: meteringPointType?.Name,
                settlementMethod: settlementMethod?.Name,
                measureUnitType: MeasurementUnit.Kwh.Name,
                resolution: R.FromName(result.Resolution.ToString()).Name,
                energySupplierNumber: aggregatedTimeSeriesQueryParameters.EnergySupplierId,
                balanceResponsibleNumber: aggregatedTimeSeriesQueryParameters.BalanceResponsibleId,
                period: new Period(result.PeriodStart, result.PeriodEnd),
                points: result.TimeSeriesPoints.Select(x => new AcceptedEnergyResultMessagePoint(
                    Position: 1, // Where does this come from? It used to come from AcceptedEnergyResultTimeSeriesCommand.
                    Quantity: x.Quantity,
                    QuantityQuality: x.Qualities,
                    SampleTime: string.Empty)), // Where does this come from? It used to come from AcceptedEnergyResultTimeSeriesCommand.
                businessReasonName: businessReason.Name,
                calculationResultVersion: 1,
                originalTransactionIdReference: TransactionId.From(originalTransactionId),
                settlementVersion: settlementVersion?.Name,
                relatedToMessageId: null);

            // 3d. Enqueue the message
            await _outgoingMessagesClient.EnqueueAsync(acceptedEnergyResult, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
