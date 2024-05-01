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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Application.Factories;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.AggregatedMeasureDataRequestMessageParsers;

public class XmlMessageParser : IMessageParser
{
    private const string SeriesRecordElementName = "Series";
    private const string HeaderElementName = "RequestAggregatedMeasureData_MarketDocument";
    private readonly CimXmlSchemaProvider _schemaProvider;
    private readonly List<ValidationError> _errors = new();

    public XmlMessageParser(CimXmlSchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }

    public DocumentFormat HandledFormat => DocumentFormat.Xml;

    public IncomingDocumentType DocumentType => IncomingDocumentType.RequestAggregatedMeasureData;

    public async Task<IncomingMarketMessageParserResult> ParseAsync(IIncomingMessageStream incomingMessageStream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMessageStream);

        string version;
        string businessProcessType;
        try
        {
            version = GetVersion(incomingMessageStream);
            businessProcessType = GetBusinessReason(incomingMessageStream);
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

        using var reader = XmlReader.Create(incomingMessageStream.Stream, CreateXmlReaderSettings(xmlSchema));
        if (_errors.Count > 0)
        {
            return new IncomingMarketMessageParserResult(_errors.ToArray());
        }

        try
        {
            var parsedXmlData = await ParseXmlDataAsync(reader, cancellationToken).ConfigureAwait(false);

            if (_errors.Count != 0)
            {
               return new IncomingMarketMessageParserResult(_errors.ToArray());
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

    private static IncomingMarketMessageParserResult InvalidXmlFailure(
        Exception exception)
    {
        return new IncomingMarketMessageParserResult(
            InvalidMessageStructure.From(exception));
    }

    private static string GetBusinessReason(IIncomingMessageStream message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var split = SplitNamespace(message);
        var businessReason = split[3];
        return businessReason;
    }

    private static string[] SplitNamespace(IIncomingMessageStream message)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var reader = XmlReader.Create(message.Stream);

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

    private static string GetVersion(IIncomingMessageStream message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var split = SplitNamespace(message);
        var version = split[4] + "." + split[5];
        return version;
    }

    private static async Task<IncomingMarketMessageParserResult> ParseXmlDataAsync(
        XmlReader reader, CancellationToken cancellationToken)
    {
        var root = await reader.ReadRootElementAsync().ConfigureAwait(false);
        var messageHeader = await MessageHeaderExtractor
            .ExtractAsync(reader, root, HeaderElementName, SeriesRecordElementName)
            .ConfigureAwait(false);

        var series = new List<RequestAggregatedMeasureDataMessageSeries>();
        await foreach (var serie in ParseSerieAsync(reader, root))
        {
            series.Add(serie);
        }

        return new IncomingMarketMessageParserResult(
            RequestAggregatedMeasureDataMessageFactory.Create(messageHeader, series.AsReadOnly()));
    }

    private static async IAsyncEnumerable<RequestAggregatedMeasureDataMessageSeries> ParseSerieAsync(XmlReader reader, RootElement rootElement)
    {
        var id = string.Empty;
        var marketEvaluationPointType = string.Empty;
        string? marketEvaluationSettlementMethod = null;
        var startDateAndOrTimeDateTime = string.Empty;
        var endDateAndOrTimeDateTime = string.Empty;
        string? meteringGridAreaDomainId = null;
        string? energySupplierMarketParticipantId = null;
        string? balanceResponsiblePartyMarketParticipantId = null;
        string? settlementVersion = null;
        var ns = rootElement.DefaultNamespace;

        await reader.AdvanceToAsync(SeriesRecordElementName, ns).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is(SeriesRecordElementName, ns, XmlNodeType.EndElement))
            {
                var record = CreateSerie(
                    ref id,
                    ref marketEvaluationPointType,
                    ref marketEvaluationSettlementMethod,
                    ref startDateAndOrTimeDateTime,
                    ref endDateAndOrTimeDateTime,
                    ref meteringGridAreaDomainId,
                    ref energySupplierMarketParticipantId,
                    ref balanceResponsiblePartyMarketParticipantId,
                    ref settlementVersion);
                yield return record;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.SchemaInfo?.Validity == XmlSchemaValidity.Invalid)
                await reader.ReadToEndAsync().ConfigureAwait(false);

            if (reader.Is("mRID", ns))
            {
                id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("marketEvaluationPoint.type", ns))
            {
                marketEvaluationPointType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("marketEvaluationPoint.settlementMethod", ns))
            {
                marketEvaluationSettlementMethod = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("start_DateAndOrTime.dateTime", ns))
            {
                startDateAndOrTimeDateTime = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("end_DateAndOrTime.dateTime", ns))
            {
                endDateAndOrTimeDateTime = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("meteringGridArea_Domain.mRID", ns))
            {
                meteringGridAreaDomainId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("energySupplier_MarketParticipant.mRID", ns))
            {
                energySupplierMarketParticipantId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("balanceResponsibleParty_MarketParticipant.mRID", ns))
            {
                balanceResponsiblePartyMarketParticipantId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("settlement_Series.version", ns))
            {
                settlementVersion = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }

    private static RequestAggregatedMeasureDataMessageSeries CreateSerie(
        ref string id,
        ref string? marketEvaluationPointType,
        ref string? marketEvaluationSettlementMethod,
        ref string startDateAndOrTimeDateTime,
        ref string? endDateAndOrTimeDateTime,
        ref string? meteringGridAreaDomainId,
        ref string? energySupplierMarketParticipantId,
        ref string? balanceResponsiblePartyMarketParticipantId,
        ref string? settlementVersion)
    {
        var serie = new RequestAggregatedMeasureDataMessageSeries(
            id,
            marketEvaluationPointType,
            marketEvaluationSettlementMethod,
            startDateAndOrTimeDateTime,
            endDateAndOrTimeDateTime,
            meteringGridAreaDomainId,
            energySupplierMarketParticipantId,
            balanceResponsiblePartyMarketParticipantId,
            settlementVersion);

        id = string.Empty;
        marketEvaluationPointType = null;
        marketEvaluationSettlementMethod = null;
        startDateAndOrTimeDateTime = string.Empty;
        endDateAndOrTimeDateTime = null;
        meteringGridAreaDomainId = null;
        energySupplierMarketParticipantId = null;
        balanceResponsiblePartyMarketParticipantId = null;

        return serie;
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
        _errors.Add(InvalidMessageStructure.From(message));
    }
}
