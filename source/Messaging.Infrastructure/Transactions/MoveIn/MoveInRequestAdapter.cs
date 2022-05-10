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
using System.Net.Http.Json;
using System.Threading.Tasks;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Transactions.MoveIn.Dtos;
using MoveInRequestDto = Messaging.Infrastructure.Transactions.MoveIn.Dtos.MoveInRequestDto;

namespace Messaging.Infrastructure.Transactions.MoveIn;
public sealed class MoveInRequestAdapter : IMoveInRequestAdapter
{
    private readonly Uri _moveInRequestUrl;
    private readonly HttpClient _httpClient;

    public MoveInRequestAdapter(Uri moveInRequestUrl, HttpClient httpClient)
    {
        _moveInRequestUrl = moveInRequestUrl ?? throw new ArgumentNullException(nameof(moveInRequestUrl));
        _httpClient = httpClient;
    }

    public async Task<BusinessRequestResult> InvokeAsync(MoveInRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var moveInRequestDto = new MoveInRequestDto(
            request.ConsumerName,
            request.EnergySupplierGlnNumber,
            request.SocialSecurityNumber,
            request.VATNumber,
            request.AccountingPointGsrnNumber,
            request.StartDate,
            request.TransactionId);

        var response = await _httpClient.PostAsJsonAsync(_moveInRequestUrl, moveInRequestDto).ConfigureAwait(false);
        var moveInResponseDto = await response.Content.ReadFromJsonAsync<MoveInResponseDto>().ConfigureAwait(false) ?? throw new InvalidOperationException();

        if (moveInResponseDto.ValidationErrors.Count > 0)
        {
            var validationErrors = moveInResponseDto.ValidationErrors.Select(error => new ValidationError(error.Code, error.Message));
            return BusinessRequestResult.Failure(validationErrors.ToArray());
        }

        return BusinessRequestResult.Succeeded();
    }
}
