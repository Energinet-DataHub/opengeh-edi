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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;

public abstract class XmlMessageParserBase(CimXmlSchemaProvider schemaProvider) : MessageParserBase<XmlSchema>
{
    private const string SeriesElementName = "Series";
    private const string MridElementName = "mRID";
    private const string TypeElementName = "type";
    private const string ProcessTypeElementName = "process.processType";
    private const string SenderMridElementName = "sender_MarketParticipant.mRID";
    private const string SenderRoleElementName = "sender_MarketParticipant.marketRole.type";
    private const string ReceiverMridElementName = "receiver_MarketParticipant.mRID";
    private const string ReceiverRoleElementName = "receiver_MarketParticipant.marketRole.type";
    private const string CreatedDateTimeElementName = "createdDateTime";
    private const string BusinessSectorTypeElementName = "businessSector.type";
    private readonly CimXmlSchemaProvider _schemaProvider = schemaProvider;

    public override DocumentFormat DocumentFormat => DocumentFormat.Xml;

    protected abstract string RootPayloadElementName { get; }

    private Collection<ValidationError> ValidationErrors { get; } = [];

    protected override async Task<IncomingMarketMessageParserResult> ParseMessageAsync(
        IIncomingMarketMessageStream marketMessage,
        XmlSchema schemaResult,
        CancellationToken cancellationToken)
    {
        using var reader = XmlReader.Create(marketMessage.Stream, CreateXmlReaderSettings(schemaResult));
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        if (ValidationErrors.Count > 0)
        {
            return new IncomingMarketMessageParserResult(ValidationErrors.ToArray());
        }

        var @namespace = GetNamespace(marketMessage);
        var ns = XNamespace.Get(@namespace);

        var header = ParseHeader(document, ns);
        var seriesElements = document.Descendants(ns + SeriesElementName);
        var transactions = new List<IIncomingMessageSeries>();

        foreach (var seriesElement in seriesElements)
        {
            transactions.Add(ParseTransaction(seriesElement, ns, header.SenderId));
        }

        return CreateResult(header, transactions);
    }

    protected abstract IIncomingMessageSeries ParseTransaction(XElement seriesElement, XNamespace ns, string senderNumber);

    protected override async Task<(XmlSchema? Schema, ValidationError? ValidationError)> GetSchemaAsync(IIncomingMarketMessageStream marketMessage, CancellationToken cancellationToken)
    {
        XmlSchema? xmlSchema = default;
        try
        {
            var @namespace = GetNamespace(marketMessage);
            var version = GetVersion(@namespace);
            var businessProcessType = BusinessProcessType(@namespace);
            xmlSchema = await _schemaProvider.GetSchemaAsync<XmlSchema>(businessProcessType, version, cancellationToken)
                .ConfigureAwait(true);

            if (xmlSchema is null)
            {
                return (xmlSchema, new InvalidBusinessReasonOrVersion(businessProcessType, version));
            }
        }
        catch (XmlException exception)
        {
            return (xmlSchema, InvalidMessageStructure.From(exception));
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            return (xmlSchema, InvalidMessageStructure.From(objectDisposedException));
        }
        catch (IndexOutOfRangeException indexOutOfRangeException)
        {
            return (xmlSchema, InvalidMessageStructure.From(indexOutOfRangeException));
        }

        return (xmlSchema, null);
    }

    protected abstract IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions);

    private static string[] SplitNamespace(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        return @namespace.Split(':');
    }

    private string BusinessProcessType(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        var split = SplitNamespace(@namespace);
        if (split.Length < 6)
        {
            throw new XmlException($"Invalid namespace format");
        }

        return split[3];
    }

    private string GetVersion(string @namespace)
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

    private string GetNamespace(IIncomingMarketMessageStream marketMessage)
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

    private MessageHeader ParseHeader(XDocument document, XNamespace ns)
    {
        var headerElement = document.Descendants(ns + RootPayloadElementName).SingleOrDefault();
        if (headerElement == null) throw new InvalidOperationException("Header element not found");

        var messageId = headerElement.Element(ns + MridElementName)?.Value ?? string.Empty;
        var messageType = headerElement.Element(ns + TypeElementName)?.Value ?? string.Empty;
        var processType = headerElement.Element(ns + ProcessTypeElementName)?.Value ?? string.Empty;
        var senderId = headerElement.Element(ns + SenderMridElementName)?.Value ?? string.Empty;
        var senderRole = headerElement.Element(ns + SenderRoleElementName)?.Value ?? string.Empty;
        var receiverId = headerElement.Element(ns + ReceiverMridElementName)?.Value ?? string.Empty;
        var receiverRole = headerElement.Element(ns + ReceiverRoleElementName)?.Value ?? string.Empty;
        var createdAt = headerElement.Element(ns + CreatedDateTimeElementName)?.Value ?? string.Empty;
        var businessType = headerElement.Element(ns + BusinessSectorTypeElementName)?.Value;

        return new MessageHeader(
            messageId,
            messageType,
            processType,
            senderId,
            senderRole,
            receiverId,
            receiverRole,
            createdAt,
            businessType);
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
        ValidationErrors.Add(InvalidMessageStructure.From(message));
    }
}
