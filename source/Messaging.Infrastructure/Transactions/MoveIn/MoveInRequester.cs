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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Contracts.BusinessRequests.MoveIn;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.Extensions.Logging;

namespace Messaging.Infrastructure.Transactions.MoveIn;
public sealed class MoveInRequester : IMoveInRequester
{
    private readonly MoveInConfiguration _configuration;
    private readonly ISerializer _serializer;
    private readonly IHttpClientAdapter _httpClientAdapter;
    private readonly ILogger<MoveInRequester> _logger;

    public MoveInRequester(MoveInConfiguration configuration, IHttpClientAdapter httpClientAdapter, ISerializer serializer,  ILogger<MoveInRequester> logger)
    {
        _configuration = configuration;
        _httpClientAdapter = httpClientAdapter;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task<BusinessRequestResult> InvokeAsync(MoveInRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var response = await TryCallAsync(CreateRequestFrom(request)).ConfigureAwait(false);
        return await ParseResultFromAsync(response).ConfigureAwait(false);
    }

    private static RequestV2 CreateRequestFrom(MoveInRequest request)
    {
        return new RequestV2(
            request.AccountingPointGsrnNumber,
            request.EnergySupplierGlnNumber,
            request.StartDate,
            new Customer(
                request.ConsumerName,
                request.ConsumerId));
    }

    private async Task<HttpResponseMessage> TryCallAsync(RequestV2 request)
    {
        using var content = new StringContent(_serializer.Serialize(request));
        var response = await _httpClientAdapter.PostAsync(_configuration.RequestUri, content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<BusinessRequestResult> ParseResultFromAsync(HttpResponseMessage response)
    {
        try
        {
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogInformation($"Response body from business processing: {responseBody}");

            var result = _serializer.Deserialize<Response>(responseBody);
            var validationErrors = result.ValidationErrors.ToList();
            if (validationErrors.Count > 0)
            {
                return BusinessRequestResult.Failure(validationErrors.ToArray());
            }

            return BusinessRequestResult.Succeeded(result.ProcessId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to deserialize response from business processing.");
            throw;
        }
    }
}
