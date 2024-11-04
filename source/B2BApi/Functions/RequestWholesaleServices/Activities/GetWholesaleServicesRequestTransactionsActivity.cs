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

using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Wholesale.Edi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Energinet.DataHub.EDI.B2BApi.Functions.RequestWholesaleServices.Activities;

public class GetWholesaleServicesRequestTransactionsActivity
{
    /// <summary>
    /// Start an ValidateWholesaleServicesRequestActivity activity.
    /// <remarks>The <paramref name="input"/> type and return type must be that same as the <see cref="Run"/> method</remarks>
    /// <remarks>Changing the <paramref name="input"/> or return type might break the Durable Function's deserialization</remarks>
    /// </summary>
    public static Task<IReadOnlyCollection<RequestWholesaleServicesTransaction>> StartActivityAsync(
        InitializeWholesaleServicesProcessDto input,
        TaskOrchestrationContext context,
        TaskOptions? options)
    {
        return context.CallActivityAsync<IReadOnlyCollection<RequestWholesaleServicesTransaction>>(
            nameof(GetWholesaleServicesRequestTransactionsActivity),
            input,
            options: options);
    }

    [Function(nameof(GetWholesaleServicesRequestTransactionsActivity))]
    public Task<IReadOnlyCollection<RequestWholesaleServicesTransaction>> Run([ActivityTrigger] InitializeWholesaleServicesProcessDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var transactions = InitializeWholesaleServicesProcessesHandler
            .CreateWholesaleServicesProcesses(input)
            .Select(p => new RequestWholesaleServicesTransaction(
                p.ProcessId,
                p.RequestedByActor,
                p.OriginalActor,
                p.BusinessTransactionId,
                p.InitiatedByMessageId,
                p.BusinessReason,
                p.StartOfPeriod,
                p.EndOfPeriod,
                p.RequestedGridArea,
                p.EnergySupplierId,
                p.SettlementVersion,
                p.Resolution,
                p.ChargeOwner,
                p.ChargeTypes,
                p.GridAreas))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<RequestWholesaleServicesTransaction>>(transactions);
    }
}
