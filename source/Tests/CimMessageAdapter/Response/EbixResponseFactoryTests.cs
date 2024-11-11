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
public sealed class EbixResponseFactoryTests
{
    private readonly EbixResponseFactory _responseFactory = new();

    [Fact]
    public void Given_FailureResultWithOneError_When_ResponseCreated_Then_ResponseContainsError()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var result = Result.Failure(duplicateMessageIdError);

        var response = CreateResponse(result);

        response.IsErrorResponse.Should().BeTrue();
        AssertHasValue(response, "faultcode", "soapenv:Client");
        AssertHasValue(response, "faultstring", $"{duplicateMessageIdError.EbixCode}:{duplicateMessageIdError.EbixMessage}");
    }

    [Fact]
    public void Given_FailureResult_When_ResponseCreated_Then_ResponseLooksLikeAnEbixResponse()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var result = Result.Failure(duplicateMessageIdError);

        var response = CreateResponse(result);

        response.IsErrorResponse.Should().BeTrue();
        response.MessageBody.Should()
            .BeEquivalentTo(
                """
                <Error>
                  <faultcode>soapenv:Client</faultcode>
                  <faultstring>B2B-003:The provided Ids are not unique and have been used before</faultstring>
                  <detail>
                    <fault>
                      <ErrorCode>B2B-003</ErrorCode>
                      <ErrorText>The provided Ids are not unique and have been used before</ErrorText>
                    </fault>
                  </detail>
                </Error>
                """.ReplaceLineEndings());
    }

    [Fact]
    public void Given_FailureResultWithMultipleErrors_When_ResponseCreated_Then_ResponseContainsOnlyTheFirstError()
    {
        var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
        var duplicateTransactionIdError = new DuplicateTransactionIdDetected("Fake transaction id");
        var result = Result.Failure(duplicateMessageIdError, duplicateTransactionIdError);

        var response = CreateResponse(result);

        response.IsErrorResponse.Should().BeTrue();
        AssertHasValue(response, "faultcode", "soapenv:Client");
        AssertHasValue(response, "faultstring", $"{duplicateMessageIdError.EbixCode}:{duplicateMessageIdError.EbixMessage}");
        AssertHasNotValue(response, "faultstring", $"{duplicateTransactionIdError.EbixCode}:{duplicateTransactionIdError.EbixMessage}");
    }

    private static void AssertHasValue(ResponseMessage responseMessage, string elementName, string expectedValue)
    {
        var document = XDocument.Parse(responseMessage.MessageBody);
        (document.Element("Error")?.Element(elementName)?.Value).Should().Be(expectedValue);
    }

    private static void AssertHasNotValue(ResponseMessage responseMessage, string elementName, string expectedValue)
    {
        var document = XDocument.Parse(responseMessage.MessageBody);
        (document.Element("Error")?.Element(elementName)?.Value).Should().NotBe(expectedValue);
    }

    private ResponseMessage CreateResponse(Result result)
    {
        return _responseFactory.From(result);
    }
}
