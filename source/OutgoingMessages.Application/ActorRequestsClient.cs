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

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class ActorRequestsClient(
    IOutgoingMessagesClient outgoingMessagesClient,
    IAggregatedTimeSeriesQueries aggregatedTimeSeriesQueries,
    IWholesaleServicesQueries wholeSaleServicesQueries) : IActorRequestsClient
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IAggregatedTimeSeriesQueries _aggregatedTimeSeriesQueries = aggregatedTimeSeriesQueries;
    private readonly IWholesaleServicesQueries _wholesaleServicesQueries = wholeSaleServicesQueries;

    public async Task EnqueueAggregatedMeasureDataAsync(string businessReason, AggregatedTimeSeriesQueryParameters aggregatedTimeSeriesQueryParameters)
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

            // ------------------------------------------------------------------------- //
            // ALL PROPERTIES CONTAIN DUMMY DATA UNTIL RequestCalculatedEnergyTimeSeriesAcceptedV1 IS DONE.
            // ------------------------------------------------------------------------- //
            var acceptedEnergyResult = AcceptedEnergyResultMessageDto.Create(
                receiverNumber: ActorNumber.Create("12345"),
                receiverRole: receiverRole,
                documentReceiverNumber: ActorNumber.Create("12345"),
                documentReceiverRole: documentReceiverRole,
                processId: Guid.NewGuid(),
                eventId: EventId.From(Guid.NewGuid()),
                gridAreaCode: result.GridArea,
                meteringPointType: MeteringPointType.Consumption.Name,
                settlementMethod: SettlementMethod.Flex.Name,
                measureUnitType: MeasurementUnit.Kwh.Name,
                resolution: BuildingBlocks.Domain.Models.Resolution.Hourly.Name,
                energySupplierNumber: aggregatedTimeSeriesQueryParameters.EnergySupplierId,
                balanceResponsibleNumber: aggregatedTimeSeriesQueryParameters.BalanceResponsibleId,
                period: new Period(result.PeriodStart, result.PeriodEnd),
                points: new List<AcceptedEnergyResultMessagePoint>(),
                businessReasonName: businessReason,
                calculationResultVersion: 1,
                originalTransactionIdReference: TransactionId.New(),
                settlementVersion: "settlementVersion",
                relatedToMessageId: MessageId.New());

            // 3d. Enqueue the message
            await _outgoingMessagesClient.EnqueueAsync(acceptedEnergyResult, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
