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
using System.Xml.Linq;
using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Ebix;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;

public abstract class EbixMessageParserBase(EbixSchemaProvider schemaProvider) : MessageParserBase<XmlSchema>()
{
    private const string HeaderElementName = "HeaderEnergyDocument";
    private const string EnergyContextElementName = "ProcessEnergyContext";
    private const string Identification = "Identification";
    private const string DocumentType = "DocumentType";
    private const string Creation = "Creation";
    private const string SenderEnergyParty = "SenderEnergyParty";
    private const string RecipientEnergyParty = "RecipientEnergyParty";
    private const string EnergyBusinessProcess = "EnergyBusinessProcess";
    private const string EnergyBusinessProcessRole = "EnergyBusinessProcessRole";
    private const string EnergyIndustryClassification = "EnergyIndustryClassification";
    private readonly EbixSchemaProvider _schemaProvider = schemaProvider;

    protected abstract string RootPayloadElementName { get; }

    protected override async Task<IncomingMarketMessageParserResult> ParseMessageAsync(
        IIncomingMarketMessageStream marketMessage,
        XmlSchema schemaResult,
        string @namespace,
        CancellationToken cancellationToken)
    {
        using var reader = XmlReader.Create(marketMessage.Stream, CreateXmlReaderSettings(schemaResult));
        if (Errors.Count > 0)
        {
            return new IncomingMarketMessageParserResult(Errors.ToArray());
        }

        var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        var ns = XNamespace.Get(@namespace);

        var header = ParseHeader(document, ns);
        var transactions = ParseTransactions(document, ns, header.SenderId);

        if (Errors.Count != 0)
        {
            return new IncomingMarketMessageParserResult(Errors.ToArray());
        }

        return CreateResult(header, transactions);
    }

    protected override async Task<(XmlSchema? Schema, string? Namespace, IncomingMarketMessageParserResult? Result)> GetSchemaAsync(IIncomingMarketMessageStream marketMessage, CancellationToken cancellationToken)
    {
        string? @namespace = null;
        IncomingMarketMessageParserResult? parserResult = null;
        XmlSchema? xmlSchema = default;
        try
        {
            @namespace = GetNamespace(marketMessage);
            var version = GetVersion(@namespace);
            var businessProcessType = BusinessProcessType(@namespace);
            xmlSchema = await _schemaProvider.GetSchemaAsync<XmlSchema>(businessProcessType, version, cancellationToken)
                .ConfigureAwait(true);

            if (xmlSchema is null)
            {
                parserResult = new IncomingMarketMessageParserResult(
                    new InvalidBusinessReasonOrVersion(businessProcessType, version));
            }
        }
        catch (XmlException exception)
        {
            parserResult = Invalid(exception);
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            parserResult = Invalid(objectDisposedException);
        }
        catch (IndexOutOfRangeException indexOutOfRangeException)
        {
            parserResult = Invalid(indexOutOfRangeException);
        }

        return (xmlSchema, @namespace, parserResult);
    }

    protected abstract IReadOnlyCollection<IIncomingMessageSeries> ParseTransactions(XDocument document, XNamespace ns, string senderNumber);

    protected abstract IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions);

    private static string[] SplitNamespace(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        return @namespace.Split(':');
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

    private string BusinessProcessType(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        var split = SplitNamespace(@namespace);
        if (split.Length < 5)
        {
            throw new XmlException($"Invalid namespace format");
        }

        var businessReason = split[4];
        var parts = businessReason.Split('-');
        return parts.Last();
    }

    private string GetVersion(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        var split = SplitNamespace(@namespace);
        if (split.Length < 6)
        {
            throw new XmlException($"Invalid namespace format");
        }

        var version = split[5];
        return version.StartsWith('v') ? version[1..] : version;
    }

    private MessageHeader ParseHeader(XDocument document, XNamespace ns)
    {
        var headerElement = document.Descendants(ns + HeaderElementName).SingleOrDefault();
        if (headerElement == null) throw new InvalidOperationException("Header element not found");

        var messageId = headerElement.Element(ns + Identification)?.Value ?? string.Empty;
        var messageType = headerElement.Element(ns + DocumentType)?.Value ?? string.Empty;
        var createdAt = headerElement.Element(ns + Creation)?.Value ?? string.Empty;
        var senderId = headerElement.Element(ns + SenderEnergyParty)?.Element(ns + Identification)?.Value ?? string.Empty;
        var receiverId = headerElement.Element(ns + RecipientEnergyParty)?.Element(ns + Identification)?.Value ?? string.Empty;

        var energyContextElement = document.Descendants(ns + EnergyContextElementName).FirstOrDefault();
        if (energyContextElement == null) throw new InvalidOperationException("Energy Context element not found");

        var businessReason = energyContextElement.Element(ns + EnergyBusinessProcess)?.Value ?? string.Empty;
        var senderRole = energyContextElement.Element(ns + EnergyBusinessProcessRole)?.Value ?? string.Empty;
        var businessType = energyContextElement.Element(ns + EnergyIndustryClassification)?.Value;

        return new MessageHeader(
            messageId,
            messageType,
            businessReason,
            senderId,
            senderRole,
            receiverId,
            // ReceiverRole is not specified in incoming Ebix documents
            ActorRole.MeteredDataAdministrator.Code,
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
        Errors.Add(InvalidMessageStructure.From(message));
    }
}
