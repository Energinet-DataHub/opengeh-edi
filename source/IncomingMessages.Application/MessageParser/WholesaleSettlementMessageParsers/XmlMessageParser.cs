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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.WholesaleSettlementMessageParsers;

public class XmlMessageParser : XmlBaseParser, IMessageParser
{
    private const string SeriesElementName = "Series";
    private const string HeaderElementName = "RequestWholesaleSettlement_MarketDocument";
    private readonly CimXmlSchemaProvider _schemaProvider;

    public XmlMessageParser(CimXmlSchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }

    public DocumentFormat HandledFormat => DocumentFormat.Xml;

    public IncomingDocumentType DocumentType => IncomingDocumentType.RequestWholesaleSettlement;

    public async Task<IncomingMarketMessageParserResult> ParseAsync(IIncomingMessageStream incomingMessageStream, CancellationToken cancellationToken)
    {
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

    private async Task<IncomingMarketMessageParserResult> ParseXmlDataAsync(
        XmlReader reader)
    {
        var root = await reader.ReadRootElementAsync().ConfigureAwait(false);
        var header = await MessageHeaderExtractor
            .ExtractAsync(reader, root, HeaderElementName, SeriesElementName)
            .ConfigureAwait(false);

        var series = new List<RequestWholesaleServicesSeries>();
        await foreach (var serie in ParseSerieAsync(reader, root))
        {
            series.Add(serie);
        }

        return new IncomingMarketMessageParserResult(new RequestWholesaleServicesMessage(
            header.SenderId,
            header.SenderRole,
            header.ReceiverId,
            header.ReceiverRole,
            header.BusinessReason,
            header.MessageType,
            header.MessageId,
            header.CreatedAt,
            header.BusinessType,
            series.AsReadOnly()));
    }

    private async IAsyncEnumerable<RequestWholesaleServicesSeries> ParseSerieAsync(
        XmlReader reader,
        RootElement rootElement)
    {
        var id = string.Empty;
        var startDateAndOrTimeDateTime = string.Empty;
        string? endDateAndOrTimeDateTime = null;
        string? meteringGridAreaDomainId = null;
        string? energySupplierMarketParticipantId = null;
        string? chargeTypeOwnerMarketParticipantId = null;
        string? settlementVersion = null;
        string? resolution = null;
        string? chargeType = null;
        string? chargeId = null;
        var ns = rootElement.DefaultNamespace;

        await reader.AdvanceToAsync(SeriesElementName, ns).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is(SeriesElementName, ns, XmlNodeType.EndElement))
            {
                var chargeTypes = new List<RequestWholesaleServicesChargeType>();
                if (chargeType is not null && chargeId is not null)
                {
                    var chargeTypeOwner = new RequestWholesaleServicesChargeType(chargeId, chargeType);
                    chargeTypes.Add(chargeTypeOwner);
                }

                var serie = new RequestWholesaleServicesSeries(
                    id,
                    startDateAndOrTimeDateTime,
                    endDateAndOrTimeDateTime,
                    meteringGridAreaDomainId,
                    energySupplierMarketParticipantId,
                    settlementVersion,
                    resolution,
                    chargeTypeOwnerMarketParticipantId,
                    chargeTypes);
                yield return serie;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.SchemaInfo?.Validity == XmlSchemaValidity.Invalid)
                await reader.ReadToEndAsync().ConfigureAwait(false);

            if (reader.Depth == 2 && reader.Is("mRID", ns))
            {
                id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
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
            else if (reader.Is("chargeTypeOwner_MarketParticipant.mRID", ns))
            {
                chargeTypeOwnerMarketParticipantId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("settlement_Series.version", ns))
            {
                settlementVersion = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("aggregationSeries_Period.resolution", ns))
            {
                resolution = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Depth == 3 && reader.Is("type", ns))
            {
                chargeType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Depth == 3 && reader.Is("mRID", ns))
            {
                chargeId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }
}
