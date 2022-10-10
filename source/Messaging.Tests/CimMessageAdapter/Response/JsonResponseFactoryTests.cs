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

using Messaging.CimMessageAdapter;
using Messaging.CimMessageAdapter.Errors;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Response;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Categories;

namespace Messaging.Tests.CimMessageAdapter.Response;

[UnitTest]
public class JsonResponseFactoryTests
{
    private readonly JsonResponseFactory _responseFactory;

    public JsonResponseFactoryTests()
    {
        _responseFactory = new JsonResponseFactory();
    }

    [Fact]
    public void Generates_single_error_response()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var result = Result.Failure(duplicateMessageIdError);

        var response = CreateResponse(result);

        Assert.True(response.IsErrorResponse);
        AssertHasValue(response, "Error.Code", duplicateMessageIdError.Code);
        AssertHasValue(response, "Error.Message", duplicateMessageIdError.Message);
    }

    [Fact]
    public void Generates_multiple_errors_response()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var duplicateTransactionIdError = new DuplicateTransactionIdDetected("Fake transaction id");
        var result = Result.Failure(duplicateMessageIdError, duplicateTransactionIdError);

        var response = CreateResponse(result);

        Assert.True(response.IsErrorResponse);
        AssertHasValue(response, "Error.Code", "BadRequest");
        AssertHasValue(response, "Error.Message", "Multiple errors in message");
        AssertContainsError(response, duplicateMessageIdError, "Error.Details.Errors[0].");
        AssertContainsError(response, duplicateTransactionIdError, "Error.Details.Errors[1].");
    }

    private static void AssertHasValue(ResponseMessage response, string path, string expectedValue)
    {
        var jsonObject = JObject.Parse(response.MessageBody);
        var value = (string)jsonObject.SelectToken(path);
        Assert.Equal(expectedValue, value);
    }

    private static void AssertContainsError(ResponseMessage response, ValidationError validationError, string path)
    {
        var jsonObject = JObject.Parse(response.MessageBody);
        var code = (string)jsonObject.SelectToken(path + "Code");
        var message = (string)jsonObject.SelectToken(path + "Message");

        Assert.Equal(validationError.Code, code);
        Assert.Equal(validationError.Message, message);
    }

    private ResponseMessage CreateResponse(Result result)
    {
        return _responseFactory.From(result);
    }
}
