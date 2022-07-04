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

using System.Linq;
using System.Net.Mime;
using System.Xml.Linq;
using Messaging.CimMessageAdapter.Errors;
using Messaging.CimMessageAdapter.Response;
using Xunit;
using Xunit.Categories;

namespace Messaging.CimMessageAdapter.Tests
{
    [UnitTest]
    public class XmlResponseFactoryTests
    {
        [Fact]
        public void Generate_empty_response_when_no_validation_errors_has_occurred()
        {
            var result = Result.Succeeded();
            var responseFactory = ResponseStrategy.GetResponseStrategy(MediaTypeNames.Application.Xml);

            var response = responseFactory.From(result);

            Assert.False(response.IsErrorResponse);
            Assert.Empty(response.MessageBody);
        }

        [Fact]
        public void Generates_single_error_response()
        {
            var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
            var result = Result.Failure(duplicateMessageIdError);
            var responseFactory = ResponseStrategy.GetResponseStrategy(MediaTypeNames.Application.Xml);

            var response = responseFactory.From(result);

            Assert.True(response.IsErrorResponse);
            AssertHasValue(response, "Code", duplicateMessageIdError.Code);
            AssertHasValue(response, "Message", duplicateMessageIdError.Message);
        }

        [Fact]
        public void Generates_multiple_errors_response()
        {
            var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
            var duplicateTransactionIdError = new DuplicateTransactionIdDetected("Fake transaction id");
            var result = Result.Failure(duplicateMessageIdError, duplicateTransactionIdError);
            var responseFactory = ResponseStrategy.GetResponseStrategy(MediaTypeNames.Application.Xml);

            var response = responseFactory.From(result);

            Assert.True(response.IsErrorResponse);
            AssertHasValue(response, "Code", "BadRequest");
            AssertHasValue(response, "Message", "Multiple errors in message");
            AssertContainsError(response, duplicateMessageIdError);
            AssertContainsError(response, duplicateTransactionIdError);
        }

        private static void AssertHasValue(ResponseMessage responseMessage, string elementName, string expectedValue)
        {
            var document = XDocument.Parse(responseMessage.MessageBody);
            Assert.Equal(expectedValue, document?.Element("Error")?.Element(elementName)?.Value);
        }

        private static void AssertContainsError(ResponseMessage responseMessage, ValidationError validationError)
        {
            var document = XDocument.Parse(responseMessage.MessageBody);
            var errors = document.Element("Error")?.Element("Details")?.Elements().ToList();

            Assert.Contains(errors, error => error.Element("Code")?.Value == validationError.Code);
            Assert.Contains(errors, error => error.Element("Message")?.Value == validationError.Message);
        }
    }
}
