using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;

public class MeteredDataForMeteringPointEbixDocumentWriter(IMessageRecordParser parser)
    : EbixDocumentWriter(new DocumentDetails(
            type: "DK_MeteredDataTimeSeries",
            schemaLocation: string.Empty,
            xmlNamespace: "un:unece:260:data:EEM-DK_MeteredDataTimeSeries",
            prefix: "ns0",
            typeCode: "E66"),
        parser)
{
    public override bool HandlesType(DocumentType documentType)
    {
        return documentType == DocumentType.NotifyValidatedMeasureData;
    }

    protected override async Task WriteHeaderAsync(
        OutgoingMessageHeader header,
        DocumentDetails documentDetails,
        XmlWriter writer,
        SettlementVersion? settlementVersion)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(documentDetails);

        await writer.WriteStartDocumentAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(null, "MessageContainer", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(
            prefix: null,
            localName: "MessageReference",
            ns: "urn:www:datahub:dk:b2b:v01",
            value: $"ENDK_{Guid.NewGuid():N}")
        .ConfigureAwait(false);

        await writer.WriteElementStringAsync(
            prefix: null,
            localName: "DocumentType",
            ns: "urn:www:datahub:dk:b2b:v01",
            value: documentDetails.Type.Replace("DK_", string.Empty, StringComparison.InvariantCultureIgnoreCase))
        .ConfigureAwait(false);

        await writer.WriteElementStringAsync(
            prefix: null,
            localName: "MessageType",
            ns: "urn:www:datahub:dk:b2b:v01",
            value: "XML")
        .ConfigureAwait(false);

        await writer.WriteStartElementAsync(null, "Payload", "urn:www:datahub:dk:b2b:v01").ConfigureAwait(false);
        await writer.WriteStartElementAsync(documentDetails.Prefix, documentDetails.Type, documentDetails.XmlNamespace)
            .ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "HeaderEnergyDocument", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(
            documentDetails.Prefix,
            "Identification",
            null,
            header.MessageId).ConfigureAwait(false);

        await WriteCodeWithCodeListReferenceAttributesAsync("DocumentType", documentDetails.TypeCode, writer)
            .ConfigureAwait(false);

        await writer.WriteElementStringAsync(
            documentDetails.Prefix,
            "Creation",
            null,
            header.TimeStamp.ToString())
        .ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "SenderEnergyParty", null).ConfigureAwait(false);
        await WriteGlnOrEicCodeWithAttributesAsync("Identification", header.SenderId, writer).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "RecipientEnergyParty", null).ConfigureAwait(false);
        await WriteGlnOrEicCodeWithAttributesAsync("Identification", header.ReceiverId, writer).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "ProcessEnergyContext", null).ConfigureAwait(false);

        await WriteCodeWithCodeListReferenceAttributesAsync(
            "EnergyBusinessProcess",
            EbixCode.Of(BusinessReason.FromName(header.BusinessReason)),
            writer).ConfigureAwait(false);

        await WriteCodeWithCodeListReferenceAttributesAsync(
            "EnergyBusinessProcessRole",
            EbixCode.Of(ActorRole.FromCode(header.ReceiverRole)),
            writer).ConfigureAwait(false);

        await WriteCodeWithCodeListReferenceAttributesAsync(
            "EnergyIndustryClassification",
            GeneralValues.SectorTypeCode,
            writer).ConfigureAwait(false);

        await writer.WriteElementStringAsync(
            documentDetails.Prefix,
            "OriginalBusinessMessage",
            null,
            header.RelatedToMessageId ?? string.Empty).ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    protected override async Task WriteMarketActivityRecordsAsync(
        IReadOnlyCollection<string> marketActivityPayloads,
        XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var forwardMeteredDataRecord in ParseFrom<MeteredDateForMeteringPointMarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadEnergyTimeSeries", null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "Identification",
                null,
                forwardMeteredDataRecord.TransactionId.Value).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Function", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
            await writer.WriteStringAsync("9").ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ObservationTimeSeriesPeriod", null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "ResolutionDuration",
                null,
                forwardMeteredDataRecord.Resolution.Code)
            .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "Start",
                null,
                forwardMeteredDataRecord.StartedDateTime.ToString())
            .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "End",
                null,
                forwardMeteredDataRecord.EndedDateTime.ToString())
            .ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IncludedProductCharacteristic", null).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "9").ConfigureAwait(false);
            await writer.WriteStringAsync(forwardMeteredDataRecord.Product).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "UnitType", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteStringAsync(forwardMeteredDataRecord.QuantityMeasureUnit.Code).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "DetailMeasurementMeteringPointCharacteristic", null).ConfigureAwait(false);
            await WriteCodeWithCodeListReferenceAttributesAsync(
                "TypeOfMeteringPoint",
                EbixCode.Of(forwardMeteredDataRecord.MarketEvaluationPointType),
                writer)
            .ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MeteringPointDomainLocation", null).ConfigureAwait(false);
            await WriteGlnOrEicCodeWithAttributesAsync("Identification", forwardMeteredDataRecord.MarketEvaluationPointNumber, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            foreach (var energyObservation in forwardMeteredDataRecord.EnergyObservations)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IntervalEnergyObservation", null).ConfigureAwait(false);

                await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "Position",
                    null,
                    energyObservation.Position.ToString())
                .ConfigureAwait(false);

                await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "EnergyQuantity",
                    null,
                    energyObservation.Quantity != null ? energyObservation.Quantity.Value.ToString() : "0")
                .ConfigureAwait(false);

                if (energyObservation.Quality != null)
                {
                    await WriteCodeWithCodeListReferenceAttributesAsync("QuantityQuality", energyObservation.Quality.Code, writer).ConfigureAwait(false);
                }
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
