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

using System.Text.Encodings.Web;
using BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Response;

[UnitTest]
public sealed class JsonResponseFactoryTests
{
    private readonly JsonResponseFactory _responseFactory;

    public JsonResponseFactoryTests()
    {
        var javaScriptEncoder = new ServiceCollection()
            .AddJavaScriptEncoder()
            .BuildServiceProvider()
            .GetRequiredService<JavaScriptEncoder>();

        _responseFactory = new JsonResponseFactory(javaScriptEncoder);
    }

    [Fact]
    public void Given_FailureResultWithSingleError_When_ResponseCreated_Then_ResponseContainsError()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var result = Result.Failure(duplicateMessageIdError);

        var response = CreateResponse(result);

        response.IsErrorResponse.Should().BeTrue();
        AssertHasValue(response, "Error.Code", duplicateMessageIdError.Code);
        AssertHasValue(response, "Error.Message", duplicateMessageIdError.Message);
    }

    [Fact]
    public void Given_FailureResultWithMultipleErrors_When_ResponseCreated_Then_ResponseContainsAllErrors()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var duplicateTransactionIdError = new DuplicateTransactionIdDetected("Fake transaction id");
        var result = Result.Failure(duplicateMessageIdError, duplicateTransactionIdError);

        var response = CreateResponse(result);

        response.IsErrorResponse.Should().BeTrue();
        AssertHasValue(response, "Error.Code", "BadRequest");
        AssertHasValue(response, "Error.Message", "Multiple errors in message");
        AssertContainsError(response, duplicateMessageIdError, "Error.Details.Errors[0].");
        AssertContainsError(response, duplicateTransactionIdError, "Error.Details.Errors[1].");
    }

    [Fact]
    public void Given_FailureResultWithMultipleErrors_When_ResponseCreated_Then_ResponseLooksLikeAJsonResponse()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var duplicateTransactionIdError = new DuplicateTransactionIdDetected("Fake transaction id");
        var result = Result.Failure(duplicateMessageIdError, duplicateTransactionIdError);

        var response = CreateResponse(result);

        response.IsErrorResponse.Should().BeTrue();
        response.MessageBody.Should()
            .BeEquivalentTo(
                """
                {
                  "Error": {
                    "Code": "BadRequest",
                    "Message": "Multiple errors in message",
                    "Target": "",
                    "Details": {
                      "Errors": [
                        {
                          "Code": "00101",
                          "Message": "Message id \u0027Duplicate message id\u0027 is not unique",
                          "Target": "MessageId"
                        },
                        {
                          "Code": "00102",
                          "Message": "Transaction id \u0027Fake transaction id\u0027 is not unique and will not be processed.",
                          "Target": "TransactionId"
                        }
                      ]
                    }
                  }
                }
                """.ReplaceLineEndings());
    }

    private static void AssertHasValue(ResponseMessage response, string path, string expectedValue)
    {
        var jsonObject = JObject.Parse(response.MessageBody);
        var value = (string)jsonObject.SelectToken(path)!;
        value.Should().Be(expectedValue);
    }

    private static void AssertContainsError(ResponseMessage response, ValidationError validationError, string path)
    {
        var jsonObject = JObject.Parse(response.MessageBody);
        var code = (string)jsonObject.SelectToken(path + "Code")!;
        var message = (string)jsonObject.SelectToken(path + "Message")!;

        code.Should().Be(validationError.Code);
        message.Should().Be(validationError.Message);
    }

    private ResponseMessage CreateResponse(Result result)
    {
        return _responseFactory.From(result);
    }
}
