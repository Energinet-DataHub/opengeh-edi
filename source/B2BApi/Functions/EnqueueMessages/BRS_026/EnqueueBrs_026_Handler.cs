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
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026.V1.Model;
using Energinet.DataHub.Wholesale.Edi.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;

/// <summary>
/// Enqueue accepted/rejected messages for BRS-026 (RequestAggregatedMeasureData).
/// </summary>
/// <param name="logger"></param>
public class EnqueueBrs_026_Handler(
    ILogger<EnqueueBrs_026_Handler> logger,
    IAggregatedTimeSeriesQueries aggregatedTimeSeriesQueries,
    IOutgoingMessagesClient outgoingMessagesClient,
    IProcessManagerMessageClient processManagerMessageClient)
    : EnqueueValidatedMessagesHandlerBase<RequestCalculatedEnergyTimeSeriesAcceptedV1, RequestCalculatedEnergyTimeSeriesRejectedV1>(logger)
{
    private readonly ILogger _logger = logger;
    private readonly IAggregatedTimeSeriesQueries _aggregatedTimeSeriesQueries = aggregatedTimeSeriesQueries;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;

    protected override async Task EnqueueAcceptedMessagesAsync(RequestCalculatedEnergyTimeSeriesAcceptedV1 acceptedData)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 026. Data: {0}",
            acceptedData);

        // 1. Create actual properties in RequestCalculatedEnergyTimeSeriesAcceptedV1
        // Dummy response from PM after async validation
        var t = new AcceptedEnergyResultTimeSeries(
            Points: [new Point(0, 5, CalculatedQuantityQuality.Estimated, string.Empty)],
            MeteringPointType: MeteringPointType.Consumption,
            SettlementMethod: SettlementMethod.Flex,
            UnitType: MeasurementUnit.Kwh,
            Resolution: Resolution.Hourly,
            GridAreaCode: "804",
            CalculationResultVersion: 124123,
            StartOfPeriod: Instant.FromUtc(2024, 1, 31, 23, 00),
            EndOfPeriod: Instant.FromUtc(2024, 2, 1, 23, 00),
            SettlementVersion: null);

        var t2 = new AggregatedTimeSeriesRequest();

        // 1. Map to AggregatedTimeSeriesQueryParameters
        // 2. Call IActorRequestsClient.EnqueueAggregatedMeasureDataAsync();

        // ----- START IActorRequestsClient.EnqueueAggregatedMeasureDataAsync -----
        // 3a. Create query
        // Map AcceptedEnergyResultTimeSeries to AggregatedTimeSeriesQueryParameters

        // 3b. Get result from query
        // GetAsync(AggregatedTimeSeriesQueryParameters parameters) in IAggregatedTimeSeriesQueries
        //   -> if empty result, send NoDataRejectedMessage

        // 3c. Enqueue message
        // EnqueueAsync(AcceptedEnergyResultMessageDto resultMessage) in IOutgoingMessagesClient
        // ----- END IActorRequestsClient.EnqueueAggregatedMeasureDataAsync -----

        // 4. Notify ProcessManager
        // NotifyProcessOrchestrationAsync(NotifyOrchestrationInstanceEvent event) in IProcessManagerMessageClient

        // TODO: Call actual logic that enqueues accepted messages instead
        await Task.CompletedTask.ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(RequestCalculatedEnergyTimeSeriesRejectedV1 rejectedData)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 026. Data: {0}",
            rejectedData);

        // TODO: Call actual logic that enqueues rejected message
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
