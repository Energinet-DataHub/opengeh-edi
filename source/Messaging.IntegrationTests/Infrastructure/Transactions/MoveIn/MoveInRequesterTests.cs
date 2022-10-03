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
using System.Net.Http;
using System.Threading.Tasks;
using Contracts.BusinessRequests.MoveIn;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.Transactions;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Microsoft.Extensions.Logging;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Transactions.MoveIn;

public class MoveInRequesterTests : TestBase
{
    private readonly HttpClientSpy _httpClientSpy;
    private readonly IMoveInRequester _requestService;

    public MoveInRequesterTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _httpClientSpy = (HttpClientSpy)GetService<IHttpClientAdapter>();
        _requestService = GetService<IMoveInRequester>();
    }

    [Fact]
    public async Task Request_is_send_to_processing()
    {
        var request = CreateRequest();

        await _requestService.InvokeAsync(request).ConfigureAwait(false);

        _httpClientSpy
            .AssertJsonContent(
                new RequestV2(
                    request.AccountingPointGsrnNumber,
                    request.EnergySupplierGlnNumber,
                    request.StartDate,
                    new Customer(request.ConsumerName, request.ConsumerId)));
    }

    [Fact]
    public async Task Throw_when_business_processing_request_is_unsuccessful()
    {
        _httpClientSpy.RespondWith(HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<HttpRequestException>(() => _requestService.InvokeAsync(CreateRequest()));
    }

    private static MoveInRequest CreateRequest()
    {
        return new MoveInRequest(
            "Consumer1",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            SystemClock.Instance.GetCurrentInstant().ToString(),
            Guid.NewGuid().ToString(),
            "CPR");
    }
}
