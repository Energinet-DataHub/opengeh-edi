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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.Extensions.Logging;

namespace Messaging.Infrastructure.Transactions.MoveIn;
public sealed class MoveInRequestAdapter : IMoveInRequestAdapter
{
    private readonly Uri _moveInRequestUrl;
    private readonly HttpClient _httpClient;
    private readonly ISerializer _serializer;
    private readonly ILogger<MoveInRequestAdapter> _logger;

    public MoveInRequestAdapter(
        Uri moveInRequestUrl,
        HttpClient httpClient,
        ISerializer serializer,
        ILogger<MoveInRequestAdapter> logger)
    {
        _moveInRequestUrl = moveInRequestUrl ?? throw new ArgumentNullException(nameof(moveInRequestUrl));
        _httpClient = httpClient;
        _serializer = serializer;
        _logger = logger;
    }

    public Task<BusinessRequestResult> InvokeAsync(MoveInRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        return InvokeInternalAsync(request);
    }

    private async Task<BusinessRequestResult> InvokeInternalAsync(MoveInRequest request)
    {
        var moveInRequestDto = new MoveInRequestDto(
            request.ConsumerName,
            request.EnergySupplierGlnNumber,
            request.AccountingPointGsrnNumber,
            request.StartDate,
            request.TransactionId,
            request.ConsumerId,
            request.ConsumerIdType);

        var response = await MoveInAsync(moveInRequestDto).ConfigureAwait(false);
        var moveInResponseDto = await ParseFromAsync(response).ConfigureAwait(false);

        return moveInResponseDto.ValidationErrors.Count > 0 ? BusinessRequestResult.Failure(moveInResponseDto.ValidationErrors.ToArray()) : BusinessRequestResult.Succeeded();
    }

    private async Task<HttpResponseMessage> MoveInAsync(MoveInRequestDto moveInRequestDto)
    {
        using var ms = new MemoryStream();
        await _serializer.SerializeAsync(ms, moveInRequestDto).ConfigureAwait(false);
        ms.Position = 0;
        using var content = new StreamContent(ms);
        return await _httpClient.PostAsync(_moveInRequestUrl, content).ConfigureAwait(false);
    }

    private async Task<BusinessProcessResponse> ParseFromAsync(HttpResponseMessage response)
    {
        try
        {
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogInformation($"Response body from business processing: {responseBody}");
            return _serializer.Deserialize<BusinessProcessResponse>(responseBody);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to deserialize response from business processing.");
            throw;
        }
    }
}

public record MoveInRequestDto(
    string? ConsumerName,
    string? EnergySupplierGlnNumber,
    string AccountingPointGsrnNumber,
    string StartDate,
    string TransactionId,
    string? ConsumerId,
    string? ConsumerIdType);

public record BusinessProcessResponse(IReadOnlyCollection<string> ValidationErrors);
