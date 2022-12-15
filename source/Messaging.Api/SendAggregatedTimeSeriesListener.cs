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
using System.Net;
using System.Threading.Tasks;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.Transactions.AggregatedTimeSeries;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Messaging.Api;

public class SendAggregatedTimeSeriesListener
{
    private readonly ISerializer _serializer;

    public SendAggregatedTimeSeriesListener(ISerializer serializer)
    {
        _serializer = serializer;
    }

    [Function("SendAggregatedTimeSeries")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);

        var timeSeries = await _serializer.DeserializeAsync(request.Body, typeof(TimeSeries)).ConfigureAwait(false);

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        return response;
    }
}
