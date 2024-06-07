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

using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.CimXml;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.BaseParsers;

public abstract class XmlBaseParser : IMarketMessageParser
{
    private readonly CimXmlSchemaProvider _schemaProvider;

    protected XmlBaseParser(CimXmlSchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }

    public DocumentFormat HandledFormat => DocumentFormat.Xml;

    public abstract IncomingDocumentType DocumentType { get; }

    private Collection<ValidationError> Errors { get; } = new();

    public async Task<IncomingMarketMessageParserResult> ParseAsync(IIncomingMarketMessageStream incomingMarketMessageStream, CancellationToken cancellationToken)
    {
        string version;
        string businessProcessType;
        try
        {
            version = GetVersion(incomingMarketMessageStream);
            businessProcessType = GetBusinessReason(incomingMarketMessageStream);
        }
        catch (XmlException exception)
        {
            return InvalidXmlFailure(exception);
        }
        catch (ObjectDisposedException generalException)
        {
            return InvalidXmlFailure(generalException);
        }
        catch (IndexOutOfRangeException indexOutOfRangeException)
        {
            return InvalidXmlFailure(indexOutOfRangeException);
        }

        var xmlSchema = await _schemaProvider.GetSchemaAsync<XmlSchema>(businessProcessType, version, cancellationToken)
            .ConfigureAwait(true);
        if (xmlSchema is null)
        {
            return new IncomingMarketMessageParserResult(
                new InvalidBusinessReasonOrVersion(businessProcessType, version));
        }

        using var reader = XmlReader.Create(incomingMarketMessageStream.Stream, CreateXmlReaderSettings(xmlSchema));
        if (Errors.Count > 0)
        {
            return new IncomingMarketMessageParserResult(Errors.ToArray());
        }

        try
        {
            var parsedXmlData = await ParseXmlDataAsync(reader).ConfigureAwait(false);

            if (Errors.Count != 0)
            {
                return new IncomingMarketMessageParserResult(Errors.ToArray());
            }

            return parsedXmlData;
        }
        catch (XmlException exception)
        {
            return InvalidXmlFailure(exception);
        }
        catch (ObjectDisposedException generalException)
        {
            return InvalidXmlFailure(generalException);
        }
    }

    protected static string GetVersion(IIncomingMarketMessageStream marketMessage)
    {
        ArgumentNullException.ThrowIfNull(marketMessage);
        var split = SplitNamespace(marketMessage);
        var version = split[4] + "." + split[5];
        return version;
    }

    protected static string GetBusinessReason(IIncomingMarketMessageStream marketMessage)
    {
        ArgumentNullException.ThrowIfNull(marketMessage);
        var split = SplitNamespace(marketMessage);
        var businessReason = split[3];
        return businessReason;
    }

    protected static IncomingMarketMessageParserResult InvalidXmlFailure(
        Exception exception)
    {
        return new IncomingMarketMessageParserResult(
            InvalidMessageStructure.From(exception));
    }

    protected XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                              XmlSchemaValidationFlags.ReportValidationWarnings,
        };

        settings.Schemas.Add(xmlSchema);
        settings.ValidationEventHandler += OnValidationError;
        return settings;
    }

    protected abstract Task<IncomingMarketMessageParserResult> ParseXmlDataAsync(
        XmlReader reader);

    private static string[] SplitNamespace(IIncomingMarketMessageStream marketMessage)
    {
        ArgumentNullException.ThrowIfNull(marketMessage);

        using var reader = XmlReader.Create(marketMessage.Stream);

        var split = Array.Empty<string>();
        while (reader.Read())
        {
            if (string.IsNullOrEmpty(reader.NamespaceURI)) continue;
            var @namespace = reader.NamespaceURI;
            split = @namespace.Split(':');
            break;
        }

        return split;
    }

    private void OnValidationError(object? sender, ValidationEventArgs arguments)
    {
        var message =
            $"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}.";
        Errors.Add(InvalidMessageStructure.From(message));
    }
}
