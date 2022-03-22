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
using System.Threading.Tasks;
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

            AssertHasValue(response, "Code", duplicateMessageIdError.Code);
            AssertHasValue(response, "Message", duplicateMessageIdError.Message);
        }

        private static void AssertHasValue(Response response, string elementName, string expectedValue)
        {
            var document = XDocument.Parse(response.MessageBody);
            Assert.Equal(expectedValue, document?.Element("Error")?.Element(elementName)?.Value);
        }
    }

    #pragma warning disable
    public class ResponseFactory
    {
        public static Response From(Result result)
        {
            var messageBody = $"<Error>" +
                                    $"<Code>{ result.Errors.First().Code }</Code>" +
                                    $"<Message>{result.Errors.First().Message}</Message>" +
                                    "<InnerError>" +
                                        "<Code>MessageIdPreviousUsed</Code>" +
                                        "<Message-Id>gs8u033bqn</Message-Id>" +
                                        "<Used-on>2018-05-16T15:32:12Z</Used-on>" +
                                    "</InnerError>" +
                                "</Error>";
            return new Response(messageBody);
        }
    }

    public class Response
    {
        public Response(string messageBody)
        {
            MessageBody = messageBody;
        }

        public string MessageBody { get; }
    }
}
