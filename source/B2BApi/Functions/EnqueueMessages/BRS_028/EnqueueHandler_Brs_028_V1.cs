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

using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Components.Datahub.ValueObjects;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;
using Resolution = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Resolution;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_028;

/// <summary>
/// Enqueue accepted/rejected messages for BRS-028 (RequestWholesaleServices).
/// </summary>
/// <param name="logger"></param>
public class EnqueueHandler_Brs_028_V1(
    ILogger<EnqueueHandler_Brs_028_V1> logger,
    IActorRequestsClient actorRequestsClient,
    IProcessManagerMessageClient processManagerMessageClient)
    : EnqueueActorMessagesValidatedHandlerBase<RequestCalculatedWholesaleServicesAcceptedV1, RequestCalculatedWholesaleServicesRejectedV1>(logger)
{
    private readonly ILogger _logger = logger;
    private readonly IActorRequestsClient _actorRequestsClient = actorRequestsClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;

    protected override async Task EnqueueAcceptedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        RequestCalculatedWholesaleServicesAcceptedV1 acceptedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 028. Data: {0}",
            acceptedData);

        // TODO: Call actual logic that enqueues accepted messages instead
        var queryParams = new WholesaleServicesQueryParameters(
            acceptedData.Resolution == null
                ? AmountType.TotalMonthlyAmount
                : acceptedData.Resolution == Resolution.Monthly
                    ? AmountType.TotalMonthlyAmount
                    : AmountType.AmountPerCharge,
            acceptedData.GridAreas,
            acceptedData.EnergySupplierNumber?.Value,
            acceptedData.ChargeOwnerNumber?.Value,
            acceptedData.ChargeTypes.Select(
                    ct => (ct.ChargeCode, ct.ChargeType))
                .ToList(),
            GetCalculationType(acceptedData.BusinessReason, acceptedData.SettlementVersion),
            new Period(acceptedData.PeriodStart.ToInstant(), acceptedData.PeriodEnd.ToInstant()),
            acceptedData.RequestedForActorRole == ActorRole.EnergySupplier,
            acceptedData.RequestedForActorNumber.Value);

        await _actorRequestsClient.EnqueueWholesaleServicesAsync(
                ,

            )
            .ConfigureAwait(false);

        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent(
                    OrchestrationInstanceId: orchestrationInstanceId.ToString(),
                    RequestCalculatedWholesaleServicesNotifyEventsV1.EnqueueActorMessagesCompleted),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    private CalculationType? GetCalculationType(BusinessReason businessReason, SettlementVersion? settlementVersion)
    {
        if (businessReason == BusinessReason.BalanceFixing)
            return CalculationType.BalanceFixing;
        else if (businessReason == BusinessReason.PreliminaryAggregation)
            return CalculationType.Aggregation;
        else if (businessReason == BusinessReason.WholesaleFixing)
            return CalculationType.WholesaleFixing;
        else if (businessReason == BusinessReason.Correction)
        {
            if (settlementVersion is null)
                return
        }
    }

    protected override async Task EnqueueRejectedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        RequestCalculatedWholesaleServicesRejectedV1 rejectedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 028. Data: {0}",
            rejectedData);

        // TODO: Call actual logic that enqueues rejected message

        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent(
                    OrchestrationInstanceId: orchestrationInstanceId.ToString(),
                    RequestCalculatedWholesaleServicesNotifyEventsV1.EnqueueActorMessagesCompleted),
                CancellationToken.None)
            .ConfigureAwait(false);
    }
}
