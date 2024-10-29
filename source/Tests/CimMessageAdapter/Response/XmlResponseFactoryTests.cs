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

using System.Xml.Linq;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Response;

[UnitTest]
public sealed class XmlResponseFactoryTests
{
    private readonly XmlResponseFactory _responseFactory = new();

    [Fact]
    public void Given_FailureResultWithSingleError_When_ResponseCreated_Then_ResponseContainsError()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var result = Result.Failure(duplicateMessageIdError);

        var response = CreateResponse(result);

        response.IsErrorResponse.Should().BeTrue();
        AssertHasValue(response, "Code", duplicateMessageIdError.Code);
        AssertHasValue(response, "Message", duplicateMessageIdError.Message);
    }

    [Fact]
    public void Given_FailureResultWithMultipleErrors_When_ResponseCreated_Then_ResponseContainsAllErrors()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var duplicateTransactionIdError = new DuplicateTransactionIdDetected("Fake transaction id");
        var result = Result.Failure(duplicateMessageIdError, duplicateTransactionIdError);

        var response = CreateResponse(result);

        response.IsErrorResponse.Should().BeTrue();
        AssertHasValue(response, "Code", "BadRequest");
        AssertHasValue(response, "Message", "Multiple errors in message");
        AssertContainsError(response, duplicateMessageIdError);
        AssertContainsError(response, duplicateTransactionIdError);
    }

    [Fact]
    public void Given_FailureResultWithMultipleErrors_When_ResponseCreated_Then_ResponseLooksLikeAXmlResponse()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var duplicateTransactionIdError = new DuplicateTransactionIdDetected("Fake transaction id");
        var result = Result.Failure(duplicateMessageIdError, duplicateTransactionIdError);

        var response = CreateResponse(result);

        response.IsErrorResponse.Should().BeTrue();
        response.MessageBody.Should()
            .BeEquivalentTo(
                """
                <Error>
                  <Code>BadRequest</Code>
                  <Message>Multiple errors in message</Message>
                  <Target />
                  <Details>
                    <Error>
                      <Code>00101</Code>
                      <Message>Message id 'Duplicate message id' is not unique</Message>
                      <Target>MessageId</Target>
                    </Error>
                    <Error>
                      <Code>00102</Code>
                      <Message>Transaction id 'Fake transaction id' is not unique and will not be processed.</Message>
                      <Target>TransactionId</Target>
                    </Error>
                  </Details>
                </Error>
                """);
    }

    private static void AssertHasValue(ResponseMessage responseMessage, string elementName, string expectedValue)
    {
        var document = XDocument.Parse(responseMessage.MessageBody);
        (document.Element("Error")?.Element(elementName)?.Value).Should().Be(expectedValue);
    }

    private static void AssertContainsError(ResponseMessage responseMessage, ValidationError validationError)
    {
        var document = XDocument.Parse(responseMessage.MessageBody);
        var errors = document.Element("Error")?.Element("Details")?.Elements().ToList();

        Assert.Contains(errors!, error => error.Element("Code")?.Value == validationError.Code);
        Assert.Contains(errors!, error => error.Element("Message")?.Value == validationError.Message);
    }

    private ResponseMessage CreateResponse(Result result)
    {
        return _responseFactory.From(result);
    }
}
