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

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.FeatureFlag;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.EDI.Api.IncomingMessages;

public class RequestWholesaleSettlementReceiver
{
    private readonly IFeatureFlagManager _featureFlagManager;

    public RequestWholesaleSettlementReceiver(IFeatureFlagManager featureFlagManager)
    {
        _featureFlagManager = featureFlagManager;
    }

    [Function(nameof(RequestAggregatedMeasureMessageReceiver))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData request,
        CancellationToken hostCancellationToken)
    {
        if (!await _featureFlagManager.UseRequestWholesaleSettlementReceiver.ConfigureAwait(false))
        {
            return request.CreateResponse(HttpStatusCode.NotFound);
        }

        return request.CreateResponse(HttpStatusCode.OK);
    }
}
