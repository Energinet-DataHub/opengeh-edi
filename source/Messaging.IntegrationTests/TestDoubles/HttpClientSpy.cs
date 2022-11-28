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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Contracts.BusinessRequests.MoveIn;
using Messaging.Infrastructure.Transactions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Messaging.IntegrationTests.TestDoubles;

public class HttpClientSpy : IHttpClientAdapter
{
    private readonly List<string> _validationErrors = new();
    private string _messageBody = string.Empty;
    private HttpStatusCode _responseCode = HttpStatusCode.OK;
    private string _businessProcessId = Guid.NewGuid().ToString();

    public void AssertJsonContent(object expectedContent)
    {
        Assert.True(JToken.DeepEquals(JToken.Parse(JsonConvert.SerializeObject(expectedContent)), JToken.Parse(_messageBody)));
    }

    public async Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        _messageBody = await content.ReadAsStringAsync();
        return CreateResponseFromProcessing();
    }

    public void RespondWith(HttpStatusCode responseCode)
    {
        _responseCode = responseCode;
    }

    public void RespondWithValidationErrors(IEnumerable<string> validationErrors)
    {
        _validationErrors.AddRange(validationErrors);
    }

    public void RespondWithBusinessProcessId(Guid businessProcessId)
    {
        _businessProcessId = businessProcessId.ToString();
    }

    private HttpResponseMessage CreateResponseFromProcessing()
    {
        var businessProcessResponse = new Response(_validationErrors, _businessProcessId);
        var content = new StringContent(JsonConvert.SerializeObject(businessProcessResponse));
        var response = new HttpResponseMessage(_responseCode);
        response.Content = content;
        return response;
    }
}
