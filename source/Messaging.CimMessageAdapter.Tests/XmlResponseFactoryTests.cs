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
        private readonly XmlResponseFactory _responseFactory;

        public XmlResponseFactoryTests()
        {
            _responseFactory = new XmlResponseFactory();
        }

        [Fact]
        public void Generates_single_error_response()
        {
            var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
            var result = Result.Failure(duplicateMessageIdError);

            var response = CreateResponse(result);

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

            var response = CreateResponse(result);

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

        private ResponseMessage CreateResponse(Result result)
        {
            return _responseFactory.From(result);
        }
    }
}
