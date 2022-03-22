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
using System.Xml;
using System.Xml.Linq;
using B2B.CimMessageAdapter;
using B2B.CimMessageAdapter.Errors;
using Xunit;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests
{
    public class ResponseFactoryTests
    {
        private readonly ResponseFactory _responseFactory = new();

        [Fact]
        public void Generates_single_error_response()
        {
            var duplicateMessageIdError = new DuplicateMessageIdDetected("Duplicate message id");
            var result = Result.Failure(duplicateMessageIdError);

            var response = _responseFactory.From(result);

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

            var response = _responseFactory.From(result);

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
        public Response From(Result result)
        {
            return new Response(true, CreateMessageBodyFrom(result));
        }

        private string CreateMessageBodyFrom(Result result)
        {
            var messageBody = new StringBuilder();
            var settings = new XmlWriterSettings() { OmitXmlDeclaration = true, };

            using var writer = XmlWriter.Create(messageBody, settings);
            writer.WriteStartElement("Error");
            writer.WriteElementString("Code", result.Errors.Count == 1 ? result.Errors.First().Code : "BadRequest");
            writer.WriteElementString("Message", result.Errors.Count == 1 ? result.Errors.First().Message : "Multiple errors in message");
            if (result.Errors.Count > 1)
            {
                writer.WriteStartElement("Details");
                foreach (var validationError in result.Errors)
                {
                    writer.WriteStartElement("Error");
                    writer.WriteElementString("Code", validationError.Code);
                    writer.WriteElementString("Message", validationError.Message);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.Close();

            return messageBody.ToString();
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
