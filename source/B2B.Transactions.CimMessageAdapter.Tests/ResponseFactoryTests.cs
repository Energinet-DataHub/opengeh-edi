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
using System.Text;
using System.Xml.Linq;
using B2B.CimMessageAdapter;
using B2B.CimMessageAdapter.Errors;
using Xunit;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests
{
    public class ResponseFactoryTests
    {
        [Fact]
        public void Generates_single_error_response()
        {
            var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
            var result = Result.Failure(duplicateMessageIdError);

            var response = ResponseFactory.From(result);

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

            var response = ResponseFactory.From(result);

            Assert.True(response.IsErrorResponse);
            AssertHasValue(response, "Code", "BadRequest");
            AssertHasValue(response, "Message", "Multiple errors in message");
            AssertContainsError(response, duplicateMessageIdError);
            AssertContainsError(response, duplicateTransactionIdError);
        }

        private static void AssertHasValue(Response response, string elementName, string expectedValue)
        {
            var document = XDocument.Parse(response.MessageBody);
            Assert.Equal(expectedValue, document?.Element("Error")?.Element(elementName)?.Value);
        }

        private static void AssertContainsError(Response response, ValidationError validationError)
        {
            var document = XDocument.Parse(response.MessageBody);
            var errors = document.Element("Error")?.Element("Details")?.Elements().ToList();

            Assert.Contains(errors, error => error.Element("Code")?.Value == validationError.Code);
            Assert.Contains(errors, error => error.Element("Message")?.Value == validationError.Message);
        }
    }

    #pragma warning disable
    public class ResponseFactory
    {
        public static Response From(Result result)
        {
            if (result.Errors.Count > 1)
            {
                var detailsBuilder = new StringBuilder();
                foreach (var validationError in result.Errors)
                {
                    detailsBuilder.AppendLine("<Error>");
                    detailsBuilder.AppendLine($"<Code>{validationError.Code}</Code>");
                    detailsBuilder.AppendLine($"<Message>{validationError.Message}</Message>");
                    detailsBuilder.AppendLine("</Error>");
                }

                return new Response(true, $"<Error>" +
                                    "<Code>BadRequest</Code>" +
                                    "<Message>Multiple errors in message</Message>" +
                                    $"<Details>{detailsBuilder}</Details>" +
                                    "<InnerError>" +
                                    "<Code>MessageIdPreviousUsed</Code>" +
                                    "<Message-Id>gs8u033bqn</Message-Id>" +
                                    "<Used-on>2018-05-16T15:32:12Z</Used-on>" +
                                    "</InnerError>" +
                                    "</Error>");
            }

            return new Response(true, $"<Error>" +
                                      $"<Code>{result.Errors.First().Code}</Code>" +
                                      $"<Message>{result.Errors.First().Message}</Message>" +
                                      "<InnerError>" +
                                      "<Code>MessageIdPreviousUsed</Code>" +
                                      "<Message-Id>gs8u033bqn</Message-Id>" +
                                      "<Used-on>2018-05-16T15:32:12Z</Used-on>" +
                                      "</InnerError>" +
                                      "</Error>");
        }
    }

    public class Response
    {
        internal Response(bool isErrorResponse, string messageBody)
        {
            IsErrorResponse = isErrorResponse;
            MessageBody = messageBody;
        }

        public string MessageBody { get; }
        public bool IsErrorResponse { get; }
    }
}
