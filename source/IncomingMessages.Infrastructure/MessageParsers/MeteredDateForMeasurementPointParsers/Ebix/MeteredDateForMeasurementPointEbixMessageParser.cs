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
using System.Xml.Linq;
using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Ebix;
using Microsoft.Extensions.Logging;
using MessageHeaderExtractor = Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers.Ebix.MessageHeaderExtractor;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.MeteredDateForMeasurementPointParsers.Ebix;

public class MeteredDateForMeasurementPointEbixMessageParser(EbixSchemaProvider schemaProvider, ILogger<MeteredDateForMeasurementPointEbixMessageParser> logger) : IMarketMessageParser
{
    private const string RootPayloadElementName = "DK_MeteredDataTimeSeries";
    private readonly EbixSchemaProvider _schemaProvider = schemaProvider;
    private readonly ILogger<MeteredDateForMeasurementPointEbixMessageParser> _logger = logger;

    public DocumentFormat HandledFormat => DocumentFormat.Ebix;

    public IncomingDocumentType DocumentType => IncomingDocumentType.MeteredDataForMeasurementPoint;

    private Collection<ValidationError> Errors { get; } = [];

    public async Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        CancellationToken cancellationToken)
    {
        var xmlSchemaResult = await GetSchemaAsync(incomingMarketMessageStream, cancellationToken).ConfigureAwait(false);
        if (xmlSchemaResult.Schema == null || xmlSchemaResult.Namespace == null)
        {
            return xmlSchemaResult.ParserResult ?? new IncomingMarketMessageParserResult(new InvalidSchemaOrNamespace());
        }

        using var reader = XmlReader.Create(incomingMarketMessageStream.Stream, CreateXmlReaderSettings(xmlSchemaResult.Schema));
        if (Errors.Count > 0)
        {
            return new IncomingMarketMessageParserResult(Errors.ToArray());
        }

        try
        {
            var parsedXmlData = await ParseXmlDataAsync(reader, xmlSchemaResult.Namespace, cancellationToken).ConfigureAwait(false);

            if (Errors.Count != 0)
            {
                _logger.LogError("Errors found after parsing XML data: {Errors}", Errors);
                return new IncomingMarketMessageParserResult(Errors.ToArray());
            }

            return parsedXmlData;
        }
        catch (XmlException exception)
        {
            _logger.LogError(exception, "Ebix parsing error during data extraction");
            return InvalidEbixFailure(exception);
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            _logger.LogError(objectDisposedException, "Stream was disposed during data extraction");
            return InvalidEbixFailure(objectDisposedException);
        }
    }

    private static IncomingMarketMessageParserResult InvalidEbixFailure(
        Exception exception)
    {
        return new IncomingMarketMessageParserResult(
            InvalidMessageStructure.From(exception));
    }

    private static string BusinessProcessType(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        var split = SplitNamespace(@namespace);
        if (split.Length < 5)
        {
            throw new ArgumentException("Invalid namespace format", nameof(@namespace));
        }

        var businessReason = split[4];
        var parts = businessReason.Split('-');
        return parts.Last();
    }

    private static string GetVersion(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        var split = SplitNamespace(@namespace);
        if (split.Length < 6)
        {
            throw new ArgumentException("Invalid namespace format", nameof(@namespace));
        }

        var version = split[5];
        return version.StartsWith('v') ? version[1..] : version;
    }

    private static string[] SplitNamespace(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        return @namespace.Split(':');
    }

    private static string GetNamespace(IIncomingMarketMessageStream marketMessage)
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
            if (reader.NodeType == XmlNodeType.Element && reader.Name.Contains(RootPayloadElementName))
            {
                return reader.NamespaceURI;
            }
        }

        throw new XmlException($"Namespace for element '{RootPayloadElementName}' not found.");
    }

    private async Task<IncomingMarketMessageParserResult> ParseXmlDataAsync(
        XmlReader reader,
        string @namespace,
        CancellationToken cancellationToken)
    {
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        var ns = XNamespace.Get(@namespace);

        var header = MessageHeaderExtractor.Extract(document, ns);
        var listOfSeries = MeteredDataForMeasurementPointSeriesExtractor
            .ParseSeries(document, ns, header.SenderId)
            .ToList();

        return new IncomingMarketMessageParserResult(new MeteredDataForMeasurementPointMessage(
            header.MessageId,
            header.MessageType,
            header.CreatedAt,
            header.SenderId,
            header.ReceiverId,
            header.SenderRole,
            header.BusinessReason,
            header.ReceiverRole,
            header.BusinessType,
            listOfSeries.AsReadOnly()));
    }

    private async Task<(XmlSchema? Schema, string? Namespace, IncomingMarketMessageParserResult? ParserResult)> GetSchemaAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        CancellationToken cancellationToken)
    {
        string? @namespace = null;
        IncomingMarketMessageParserResult? parserResult = null;
        XmlSchema? xmlSchema = null;
        try
        {
            @namespace = GetNamespace(incomingMarketMessageStream);
            var version = GetVersion(@namespace);
            var businessProcessType = BusinessProcessType(@namespace);
            xmlSchema = await _schemaProvider.GetSchemaAsync<XmlSchema>(businessProcessType, version, cancellationToken)
                .ConfigureAwait(true);

            if (xmlSchema is null)
            {
                _logger.LogError("Schema not found for business process type {BusinessProcessType} and version {Version}", businessProcessType, version);
                parserResult = new IncomingMarketMessageParserResult(
                    new InvalidBusinessReasonOrVersion(businessProcessType, version));
            }
        }
        catch (XmlException exception)
        {
            _logger.LogWarning(exception, "Ebix parsing error");
            parserResult = InvalidEbixFailure(exception);
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            _logger.LogWarning(objectDisposedException, "Stream was disposed");
            parserResult = InvalidEbixFailure(objectDisposedException);
        }
        catch (IndexOutOfRangeException indexOutOfRangeException)
        {
            _logger.LogWarning(indexOutOfRangeException, "Namespace format is invalid");
            parserResult = InvalidEbixFailure(indexOutOfRangeException);
        }

        return (xmlSchema, @namespace, parserResult);
    }

    private XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
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

    private void OnValidationError(object? sender, ValidationEventArgs arguments)
    {
        var message =
            $"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}.";
        Errors.Add(InvalidMessageStructure.From(message));
    }
}
