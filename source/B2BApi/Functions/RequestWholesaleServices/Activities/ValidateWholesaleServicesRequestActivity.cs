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

using Energinet.DataHub.EDI.B2BApi.Functions.RequestWholesaleServices.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Wholesale.Edi.Validation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using ChargeType = Energinet.DataHub.Edi.Requests.ChargeType;

namespace Energinet.DataHub.EDI.B2BApi.Functions.RequestWholesaleServices.Activities;

public class ValidateWholesaleServicesRequestActivity(
    IValidator<Energinet.DataHub.Edi.Requests.WholesaleServicesRequest> validator)
{
    private readonly IValidator<WholesaleServicesRequest> _validator = validator;

    /// <summary>
    /// Start an ValidateWholesaleServicesRequestActivity activity.
    /// <remarks>The <paramref name="input"/> type and return type must be that same as the <see cref="Run"/> method</remarks>
    /// <remarks>Changing the <paramref name="input"/> or return type might break the Durable Function's deserialization</remarks>
    /// </summary>
    public static Task<IReadOnlyCollection<ValidationError>> StartActivityAsync(RequestWholesaleServicesTransaction input, TaskOrchestrationContext context, TaskOptions? options)
    {
        return context.CallActivityAsync<IReadOnlyCollection<ValidationError>>(
            nameof(ValidateWholesaleServicesRequestActivity),
            input,
            options: options);
    }

    [Function(nameof(ValidateWholesaleServicesRequestActivity))]
    public async Task<IReadOnlyCollection<ValidationError>> Run([ActivityTrigger] RequestWholesaleServicesTransaction input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var wholesaleServicesRequest = CreateWholesaleServicesRequest(input);

        var validationErrors = await _validator.ValidateAsync(wholesaleServicesRequest).ConfigureAwait(false);

        return validationErrors.ToList();
    }

    private static WholesaleServicesRequest CreateWholesaleServicesRequest(RequestWholesaleServicesTransaction transaction)
    {
        var request = new WholesaleServicesRequest
        {
            RequestedForActorNumber = transaction.OriginalActor.ActorNumber.Value,
            RequestedForActorRole = transaction.OriginalActor.ActorRole.Name,
            BusinessReason = transaction.BusinessReason.Name,
            PeriodStart = transaction.StartOfPeriod,
        };

        if (transaction.EndOfPeriod != null)
            request.PeriodEnd = transaction.EndOfPeriod;

        if (transaction.Resolution != null)
            request.Resolution = Resolution.TryGetNameFromCode(transaction.Resolution, fallbackValue: transaction.Resolution);

        if (transaction.EnergySupplierId != null)
            request.EnergySupplierId = transaction.EnergySupplierId;

        if (transaction.ChargeOwner != null)
            request.ChargeOwnerId = transaction.ChargeOwner;

        if (transaction.GridAreas.Count > 0)
            request.GridAreaCodes.AddRange(transaction.GridAreas);

        if (transaction.SettlementVersion != null)
            request.SettlementVersion = transaction.SettlementVersion.Name;

        foreach (var chargeType in transaction.ChargeTypes)
        {
            var ct = new ChargeType();

            if (chargeType.Id != null)
                ct.ChargeCode = chargeType.Id;

            if (chargeType.Type != null)
                ct.ChargeType_ = MapChargeType(chargeType.Type);

            request.ChargeTypes.Add(ct);
        }

        return request;
    }

    private static string MapChargeType(string chargeType)
    {
        return BuildingBlocks.Domain.Models.ChargeType.TryGetNameFromCode(chargeType, fallbackValue: chargeType);
    }
}
