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

using System.Xml;
using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers.Xml;

public static class SchemaExtractor
{
    public static async Task<(XmlSchema? Schema, string? Namespace, IncomingMarketMessageParserResult? ParserResult)> GetXmlSchemaAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        string rootElement,
        CimXmlSchemaProvider schemaProvider,
        CancellationToken cancellationToken)
    {
        string? @namespace = null;
        IncomingMarketMessageParserResult? parserResult = null;
        XmlSchema? xmlSchema = null;
        try
        {
            @namespace = GetNamespace(incomingMarketMessageStream, rootElement);
            var version = GetVersion(@namespace);
            var businessProcessType = BusinessProcessType(@namespace);
            xmlSchema = await schemaProvider.GetSchemaAsync<XmlSchema>(businessProcessType, version, cancellationToken)
                .ConfigureAwait(true);

            if (xmlSchema is null)
            {
                parserResult = new IncomingMarketMessageParserResult(
                    new InvalidBusinessReasonOrVersion(businessProcessType, version));
            }
        }
        catch (XmlException exception)
        {
            parserResult = InvalidXmlFailure(exception);
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            parserResult = InvalidXmlFailure(objectDisposedException);
        }
        catch (IndexOutOfRangeException indexOutOfRangeException)
        {
            parserResult = InvalidXmlFailure(indexOutOfRangeException);
        }

        return (xmlSchema, @namespace, parserResult);
    }

    private static IncomingMarketMessageParserResult InvalidXmlFailure(
        Exception exception)
    {
        return new IncomingMarketMessageParserResult(
            InvalidMessageStructure.From(exception));
    }

    private static string GetNamespace(IIncomingMarketMessageStream marketMessage, string rootElement)
    {
        ArgumentNullException.ThrowIfNull(marketMessage);

        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true,
        };

        using var reader = XmlReader.Create(marketMessage.Stream, settings);
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name.Contains(rootElement))
            {
                return reader.NamespaceURI;
            }
        }

        throw new XmlException($"Namespace for element '{rootElement}' not found.");
    }

    private static string BusinessProcessType(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        var split = SplitNamespace(@namespace);
        if (split.Length < 6)
        {
            throw new XmlException($"Invalid namespace format");
        }

        return split[3];
    }

    private static string GetVersion(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        var split = SplitNamespace(@namespace);
        if (split.Length < 5)
        {
            throw new XmlException($"Invalid namespace format");
        }

        var version = split[4] + "." + split[5];
        return version;
    }

    private static string[] SplitNamespace(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        return @namespace.Split(':');
    }
}
