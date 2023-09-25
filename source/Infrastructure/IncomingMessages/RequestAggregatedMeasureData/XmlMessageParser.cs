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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.ValidationErrors;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.MarketTransactions;
using DocumentFormat = Energinet.DataHub.EDI.Domain.Documents.DocumentFormat;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public class XmlMessageParser : IMessageParser<Serie, RequestAggregatedMeasureDataMarketTransaction>
{
    private const string SeriesRecordElementName = "Series";
    private const string HeaderElementName = "RequestAggregatedMeasureData_MarketDocument";
    private readonly ISchemaProvider _schemaProvider;
    private readonly List<ValidationError> _errors = new();

    public XmlMessageParser()
    {
        _schemaProvider = new CimXmlSchemaProvider();
    }

    public DocumentFormat HandledFormat => DocumentFormat.Xml;

    public async Task<MessageParserResult<Serie, RequestAggregatedMeasureDataMarketTransaction>> ParseAsync(Stream message, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        string version;
        string businessProcessType;
        try
        {
            version = GetVersion(message);
            businessProcessType = GetBusinessReason(message);
        }
        catch (XmlException exception)
        {
            return InvalidXmlFailure(exception);
        }
        catch (ObjectDisposedException generalException)
        {
            return InvalidXmlFailure(generalException);
        }

        var xmlSchema = await _schemaProvider.GetSchemaAsync<XmlSchema>(businessProcessType, version, cancellationToken)
            .ConfigureAwait(true);
        if (xmlSchema is null)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataMarketTransaction>(
                new InvalidBusinessReasonOrVersion(businessProcessType, version));
        }

        ResetMessagePosition(message);
        using var reader = XmlReader.Create(message, CreateXmlReaderSettings(xmlSchema));
        if (_errors.Count > 0)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataMarketTransaction>(_errors.ToArray());
        }

        try
        {
            var parsedXmlData = await ParseXmlDataAsync(reader, cancellationToken).ConfigureAwait(false);

            if (_errors.Any())
            {
               return new MessageParserResult<Serie, RequestAggregatedMeasureDataMarketTransaction>(_errors.ToArray());
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

    private static MessageParserResult<Serie, RequestAggregatedMeasureDataMarketTransaction> InvalidXmlFailure(
        Exception exception)
    {
        return new MessageParserResult<Serie, RequestAggregatedMeasureDataMarketTransaction>(
            InvalidMessageStructure.From(exception));
    }

    private static string GetBusinessReason(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var split = SplitNamespace(message);
        var businessReason = split[3];
        return businessReason;
    }

    private static string[] SplitNamespace(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        ResetMessagePosition(message);
        using var reader = XmlReader.Create(message);

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

    private static void ResetMessagePosition(Stream message)
    {
        if (message.CanRead && message.Position > 0)
            message.Position = 0;
    }

    private static string GetVersion(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var split = SplitNamespace(message);
        var version = split[4] + "." + split[5];
        return version;
    }

    private static async Task<MessageParserResult<Serie, RequestAggregatedMeasureDataMarketTransaction>> ParseXmlDataAsync(
        XmlReader reader, CancellationToken cancellationToken)
    {
        var root = await reader.ReadRootElementAsync().ConfigureAwait(false);
        var messageHeader = await MessageHeaderExtractor
            .ExtractAsync(reader, root, HeaderElementName, SeriesRecordElementName, cancellationToken)
            .ConfigureAwait(false);

        var series = new List<Serie>();
        await foreach (var serie in ParseSerieAsync(reader, root))
        {
            series.Add(serie);
        }

        return new MessageParserResult<Serie, RequestAggregatedMeasureDataMarketTransaction>(
            new RequestAggregatedMeasureDataIncomingMarketDocument(messageHeader, series));
    }

    private static async IAsyncEnumerable<Serie> ParseSerieAsync(XmlReader reader, RootElement rootElement)
    {
        var id = string.Empty;
        var marketEvaluationPointType = string.Empty;
        var marketEvaluationSettlementMethod = string.Empty;
        var startDateAndOrTimeDateTime = string.Empty;
        var endDateAndOrTimeDateTime = string.Empty;
        var meteringGridAreaDomainId = string.Empty;
        var energySupplierMarketParticipantId = string.Empty;
        var balanceResponsiblePartyMarketParticipantId = string.Empty;
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
                    ref balanceResponsiblePartyMarketParticipantId);
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
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }

    private static Serie CreateSerie(
        ref string id,
        ref string marketEvaluationPointType,
        ref string marketEvaluationSettlementMethod,
        ref string startDateAndOrTimeDateTime,
        ref string endDateAndOrTimeDateTime,
        ref string meteringGridAreaDomainId,
        ref string energySupplierMarketParticipantId,
        ref string balanceResponsiblePartyMarketParticipantId)
    {
        var serie = new Serie(
            id,
            marketEvaluationPointType,
            marketEvaluationSettlementMethod,
            startDateAndOrTimeDateTime,
            endDateAndOrTimeDateTime,
            meteringGridAreaDomainId,
            energySupplierMarketParticipantId,
            balanceResponsiblePartyMarketParticipantId);

        id = string.Empty;
        marketEvaluationPointType = string.Empty;
        marketEvaluationSettlementMethod = string.Empty;
        startDateAndOrTimeDateTime = string.Empty;
        endDateAndOrTimeDateTime = string.Empty;
        meteringGridAreaDomainId = string.Empty;
        energySupplierMarketParticipantId = string.Empty;
        balanceResponsiblePartyMarketParticipantId = string.Empty;

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
