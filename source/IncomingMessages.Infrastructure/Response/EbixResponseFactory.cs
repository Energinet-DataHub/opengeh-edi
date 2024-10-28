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

using System.Text;
using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;

public class EbixResponseFactory : IResponseFactory
{
#pragma warning disable CA1822
    public DocumentFormat HandledFormat => DocumentFormat.Ebix;
#pragma warning restore CA1822

    public ResponseMessage From(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Success ? new ResponseMessage() : new ResponseMessage(CreateErrorMessageBodyFrom(result));
    }

    private static string CreateErrorMessageBodyFrom(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var messageBody = new StringBuilder();
        var settings = new XmlWriterSettings() { OmitXmlDeclaration = true, };

        using var writer = XmlWriter.Create(messageBody, settings);
        writer.WriteStartElement("Error");
        writer.WriteElementString("faultcode", result.Errors.Count == 1 ? result.Errors.First().Code : "BadRequest");
        writer.WriteElementString("faultstring", result.Errors.Count == 1 ? result.Errors.First().Message : "Multiple errors in message");
        if (result.Errors.Count > 1)
        {
            foreach (var validationError in result.Errors)
            {
                writer.WriteStartElement("detail");
                writer.WriteStartElement("fault");
                writer.WriteElementString("ErrorCode", validationError.Code);
                writer.WriteElementString("ErrorText", validationError.Message);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
        writer.Close();

        return messageBody.ToString();
    }
}
