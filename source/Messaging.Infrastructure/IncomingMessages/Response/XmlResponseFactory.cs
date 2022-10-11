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
using System.Linq;
using System.Text;
using System.Xml;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Response;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Infrastructure.IncomingMessages.Response
{
    public class XmlResponseFactory : IResponseFactory
    {
        public CimFormat HandledFormat => CimFormat.Xml;

        public ResponseMessage From(Result result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            return result.Success ? new ResponseMessage() : new ResponseMessage(CreateMessageBodyFrom(result));
        }

        private static string CreateMessageBodyFrom(Result result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
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
}
