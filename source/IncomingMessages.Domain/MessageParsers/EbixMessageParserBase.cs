﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Ebix;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;

public abstract class EbixMessageParserBase(EbixSchemaProvider schemaProvider, ILogger<EbixMessageParserBase> logger)
    : MessageParserBase<XmlSchema>()
{
    private const string HeaderElementName = "HeaderEnergyDocument";
    private const string EnergyContextElementName = "ProcessEnergyContext";
    private const string IdentificationElementName = "Identification";
    private const string DocumentTypeElementName = "DocumentType";
    private const string CreationElementName = "Creation";
    private const string SenderEnergyPartyElementName = "SenderEnergyParty";
    private const string RecipientEnergyPartyElementName = "RecipientEnergyParty";
    private const string EnergyBusinessProcessElementName = "EnergyBusinessProcess";
    private const string EnergyBusinessProcessRoleElementName = "EnergyBusinessProcessRole";
    private const string EnergyIndustryClassificationElementName = "EnergyIndustryClassification";
    private readonly EbixSchemaProvider _schemaProvider = schemaProvider;
    private readonly ILogger<EbixMessageParserBase> _logger = logger;

    protected abstract string RootPayloadElementName { get; }

    private Collection<ValidationError> ValidationErrors { get; } = [];

    protected override async Task<IncomingMarketMessageParserResult> ParseMessageAsync(
        IIncomingMarketMessageStream marketMessage,
        XmlSchema schemaResult,
        CancellationToken cancellationToken)
    {
        try
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
            var transactions = ParseTransactions(document, ns, header.SenderId, header.CreatedAt);
            return CreateResult(header, transactions);
        }
        catch (XmlSchemaValidationException e)
        {
            var streamContent = await new StreamReader(marketMessage.Stream).ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(e, "Error validating incoming message: {StreamContent}", streamContent.Substring(0, 5000));
            throw;
        }
    }

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

    /// <summary>
    /// Parse transaction.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="ns"></param>
    /// <param name="senderNumber"></param>
    /// <param name="createdAt">Transactions in EBIX doesn't contain a timestamp for when the measured data was collected/registered, so
    /// it has been decided that we should extract a timestamp from the header, and use it for each transaction.</param>
    protected abstract IReadOnlyCollection<IIncomingMessageSeries> ParseTransactions(
        XDocument document,
        XNamespace ns,
        string senderNumber,
        string createdAt);

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

        var messageId = headerElement.Element(ns + IdentificationElementName)?.Value ?? string.Empty;
        var messageType = headerElement.Element(ns + DocumentTypeElementName)?.Value ?? string.Empty;
        var createdAt = headerElement.Element(ns + CreationElementName)?.Value ?? string.Empty;
        var senderId = headerElement.Element(ns + SenderEnergyPartyElementName)?.Element(ns + IdentificationElementName)?.Value ?? string.Empty;
        var receiverId = headerElement.Element(ns + RecipientEnergyPartyElementName)?.Element(ns + IdentificationElementName)?.Value ?? string.Empty;

        var energyContextElement = document.Descendants(ns + EnergyContextElementName).FirstOrDefault();
        if (energyContextElement == null) throw new InvalidOperationException("Energy Context element not found");

        var businessReason = energyContextElement.Element(ns + EnergyBusinessProcessElementName)?.Value ?? string.Empty;
        var senderRole = energyContextElement.Element(ns + EnergyBusinessProcessRoleElementName)?.Value ?? string.Empty;
        var businessType = energyContextElement.Element(ns + EnergyIndustryClassificationElementName)?.Value;

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

        try
        {
            settings.Schemas.Add(xmlSchema);
        }
        catch (XmlSchemaException e)
        {
            _logger.LogError(e, "Error adding schema {XmlSchema} to XmlReaderSettings", xmlSchema);
            throw;
        }

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
