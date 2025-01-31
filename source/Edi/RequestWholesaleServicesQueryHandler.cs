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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using Energinet.DataHub.Wholesale.Edi.Factories;
using Energinet.DataHub.Wholesale.Edi.Mappers;
using Energinet.DataHub.Wholesale.Edi.Models;
using Microsoft.Extensions.Logging;
using Period = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Period;

namespace Energinet.DataHub.Wholesale.Edi;

/// <summary>
/// Handles retrieving WholesaleServicesRequest messages based on a incoming request
/// </summary>
public class RequestWholesaleServicesQueryHandler(
    IWholesaleServicesQueries wholesaleServicesQueries,
    WholesaleServicesRequestMapper wholesaleServicesRequestMapper,
    ILogger<WholesaleServicesRequestHandler> logger)
{
    private readonly ILogger<WholesaleServicesRequestHandler> _logger = logger;
    private readonly IWholesaleServicesQueries _wholesaleServicesQueries = wholesaleServicesQueries;
    private readonly WholesaleServicesRequestMapper _wholesaleServicesRequestMapper = wholesaleServicesRequestMapper;

    public async IAsyncEnumerable<RequestWholesaleServicesQueryResult> GetAsync(Energinet.DataHub.Edi.Requests.WholesaleServicesRequest incomingRequest, string referenceId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requests = _wholesaleServicesRequestMapper.Map(incomingRequest);

        if (requests.Count == 0)
            throw new InvalidOperationException("No mapped WholesaleServices requests found, there should always be atleast one");

        List<WholesaleServices> calculationResults = [];

        foreach (var request in requests)
        {
            var queryParameters = GetWholesaleResultQueryParameters(request);

            await foreach (var calculationResult in _wholesaleServicesQueries.GetAsync(queryParameters).WithCancellation(cancellationToken))
            {
                var result = new RequestWholesaleServicesQueryResult(
                    calculationResult,
                    RequestWholesaleServicesQueryResultEnum.Success);

                calculationResults.Add(calculationResult);

                yield return result;
            }
        }

        if (calculationResults.Count != 0)
            yield break;

        var noDataResult = await GetNoDataResultAsync(
                referenceId,
                cancellationToken,
                incomingRequest,
                GetWholesaleResultQueryParameters(requests.First()))
            .ConfigureAwait(false);

        yield return noDataResult;
    }

    private async Task<RequestWholesaleServicesQueryResult> GetNoDataResultAsync(
        string referenceId,
        CancellationToken cancellationToken,
        DataHub.Edi.Requests.WholesaleServicesRequest incomingRequest,
        WholesaleServicesQueryParameters queryParameters)
    {
        var hasDataInAnotherGridArea = await HasDataInAnotherGridAreaAsync(
                incomingRequest.RequestedForActorRole,
                queryParameters)
            .ConfigureAwait(false);

        _logger.LogInformation("No data available for WholesaleServicesRequest message with reference id {reference_id}", referenceId);

        return hasDataInAnotherGridArea
            ? new RequestWholesaleServicesQueryResult(null, RequestWholesaleServicesQueryResultEnum.NoDataForGridArea)
            : new RequestWholesaleServicesQueryResult(null, RequestWholesaleServicesQueryResultEnum.NoDataAvailable);
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Readability")]
    private WholesaleServicesQueryParameters GetWholesaleResultQueryParameters(WholesaleServicesRequest request)
    {
        return new WholesaleServicesQueryParameters(
            request.AmountType,
            request.GridAreaCodes,
            request.EnergySupplierId,
            request.ChargeOwnerId,
            request.ChargeTypes.Select(c => (c.ChargeCode, c.ChargeType)).ToList(),
            request.RequestedCalculationType == RequestedCalculationType.LatestCorrection
                ? null
                : CalculationTypeMapper.FromRequestedCalculationType(request.RequestedCalculationType),
            new Period(request.Period.Start, request.Period.End),
            request.RequestedForActorRole == DataHubNames.ActorRole.EnergySupplier,
            request.RequestedForActorNumber);
    }

    private async Task<bool> HasDataInAnotherGridAreaAsync(
        string? requestedByActorRole,
        WholesaleServicesQueryParameters queryParameters)
    {
        if (queryParameters.GridAreaCodes.Count == 0) // If grid area codes is empty, we already retrieved any data across all grid areas
            return false;

        if (requestedByActorRole is DataHubNames.ActorRole.EnergySupplier or DataHubNames.ActorRole.SystemOperator)
        {
            var queryParametersWithoutGridArea = queryParameters with
            {
                GridAreaCodes = Array.Empty<string>(),
            };

            var anyResultsExists = await _wholesaleServicesQueries.AnyAsync(queryParametersWithoutGridArea).ConfigureAwait(false);

            return anyResultsExists;
        }

        return false;
    }

    public record RequestWholesaleServicesQueryResult(WholesaleServices? WholesaleServices, RequestWholesaleServicesQueryResultEnum Result);
}
