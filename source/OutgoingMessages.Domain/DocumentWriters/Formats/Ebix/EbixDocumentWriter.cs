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

using System.Text;
using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;

public abstract class EbixDocumentWriter : IDocumentWriter
{
    public const string UnitedNationsCodeList = "6";
    public const string EbixCodeList = "260";
    public const string CountryCodeDenmark = "DK";

    public const string Gs1Code = "9";
    public const string EicCode = "305";

    private readonly DocumentDetails _documentDetails;
    private readonly IMessageRecordParser _parser;

    protected EbixDocumentWriter(DocumentDetails documentDetails, IMessageRecordParser parser)
    {
        _documentDetails = documentDetails;
        _parser = parser;
    }

    protected DocumentDetails DocumentDetails => _documentDetails;

    public virtual async Task<MarketDocumentStream> WriteAsync(
        OutgoingMessageHeader header,
        IReadOnlyCollection<string> marketActivityRecords,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = new UTF8Encoding(false), Async = true, Indent = true };
        var stream = new MarketDocumentWriterMemoryStream();
        using var writer = XmlWriter.Create(stream, settings);
        var settlementVersion = ExtractSettlementVersion(marketActivityRecords);
        await WriteHeaderAsync(header, _documentDetails, writer, settlementVersion).ConfigureAwait(false);
        await WriteMarketActivityRecordsAsync(marketActivityRecords, writer).ConfigureAwait(false);
        await WriteEndAsync(writer).ConfigureAwait(false);
        stream.Position = 0;
        return new MarketDocumentStream(stream);
    }

    public abstract bool HandlesType(DocumentType documentType);

#pragma warning disable CA1822
    public bool HandlesFormat(DocumentFormat format)
#pragma warning restore CA1822
    {
        return format == DocumentFormat.Ebix;
    }

    protected virtual SettlementVersion? ExtractSettlementVersion(IReadOnlyCollection<string> marketActivityPayloads)
    {
        return null;
    }

    protected abstract Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer);

    protected IReadOnlyCollection<TMarketActivityRecord> ParseFrom<TMarketActivityRecord>(IReadOnlyCollection<string> payloads)
    {
        ArgumentNullException.ThrowIfNull(payloads);
        var marketActivityRecords = new List<TMarketActivityRecord>();
        foreach (var payload in payloads)
        {
            marketActivityRecords.Add(_parser.From<TMarketActivityRecord>(payload));
        }

        return marketActivityRecords;
    }

    protected Task WriteElementAsync(string name, string value, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return writer.WriteElementStringAsync(DocumentDetails.Prefix, name, null, value);
    }

    /// <summary>
    /// Used to write codes that references an ebIX or UN/CEFACT codelist using listAgencyIdentifier (and listIdentifier if the codelist is country specific)
    /// </summary>
    protected async Task WriteCodeWithCodeListReferenceAttributesAsync(string name, string ebixCode, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(ebixCode);
        ArgumentNullException.ThrowIfNull(writer);

        await writer.WriteStartElementAsync(DocumentDetails.Prefix, name, null).ConfigureAwait(false);

        var isUnitedNationsCodeList = long.TryParse(ebixCode, out _);
        var isDanishEbixCode = ebixCode.StartsWith('D') && ebixCode.Length == 3;
        if (isUnitedNationsCodeList)
        {
            // UN/CEFACT code list
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, UnitedNationsCodeList).ConfigureAwait(false);
        }
        else if (isDanishEbixCode)
        {
            // Danish ebIX code list
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, EbixCodeList).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listIdentifier", null, CountryCodeDenmark).ConfigureAwait(false);
        }
        else
        {
            // ebIX code list
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, EbixCodeList).ConfigureAwait(false);
        }

        await writer.WriteStringAsync(ebixCode).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Will write the ebIX xmlnode with the schemeAgencyIdentifier attribute for a GLN number, EIC code or a GSRN number
    /// </summary>
    /// <param name="name">Name of the xmlnode</param>
    /// <param name="glnOrEicCode">GLN number, EIC code or a GSRN number</param>
    /// <param name="writer">The XmlWriter</param>
    protected async Task WriteGlnOrEicCodeWithAttributesAsync(string name, string glnOrEicCode, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(glnOrEicCode);
        ArgumentNullException.ThrowIfNull(writer);

        await writer.WriteStartElementAsync(DocumentDetails.Prefix, name, null).ConfigureAwait(false);
        if (long.TryParse(glnOrEicCode, out _))
        {
            var isGlnNumber = glnOrEicCode.Length == 13 || glnOrEicCode.Length == 18;
            var isEicCode = glnOrEicCode.Length == 16;
            if (isGlnNumber)
            {
                // GLN or GSNR number from GS1
                await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, Gs1Code).ConfigureAwait(false);
            }
            else if (isEicCode)
            {
                // EIC code
                await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, EicCode).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException($"Invalid schemecode '{glnOrEicCode}'");
            }
        }
        else
        {
            throw new InvalidOperationException($"Invalid schemecode '{glnOrEicCode}'");
        }

        await writer.WriteStringAsync(glnOrEicCode).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    protected Task WriteElementIfHasValueAsync(string name, string? value, XmlWriter writer)
    {
        if (!string.IsNullOrEmpty(value))
        {
            return WriteElementAsync(name, value, writer);
        }

        return Task.CompletedTask;
    }

    private static async Task WriteEndAsync(XmlWriter writer)
    {
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        writer.Close();
    }

    private async Task WriteHeaderAsync(
        OutgoingMessageHeader header,
        DocumentDetails documentDetails,
        XmlWriter writer,
        SettlementVersion? settlementVersion)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(documentDetails);

        await writer.WriteStartDocumentAsync().ConfigureAwait(false);

        // Write Messageconatiner
        await writer.WriteStartElementAsync(null, "MessageContainer", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(
                null,
                "MessageReference",
                "urn:www:datahub:dk:b2b:v01",
                $"ENDK_{Guid.NewGuid():N}")
            .ConfigureAwait(false);

        await writer.WriteElementStringAsync(
                null,
                "DocumentType",
                "urn:www:datahub:dk:b2b:v01",
                $"{documentDetails.Type.Replace("DK_", string.Empty, StringComparison.InvariantCultureIgnoreCase)}")
            .ConfigureAwait(false);

        await writer.WriteElementStringAsync(null, "MessageType", "urn:www:datahub:dk:b2b:v01", "XML")
            .ConfigureAwait(false);
        await writer.WriteStartElementAsync(null, "Payload", "urn:www:datahub:dk:b2b:v01").ConfigureAwait(false);
        await writer.WriteStartElementAsync(documentDetails.Prefix, documentDetails.Type, documentDetails.XmlNamespace)
            .ConfigureAwait(false);

        // Begin HeaderEnergyDocument
        await writer.WriteStartElementAsync(documentDetails.Prefix, "HeaderEnergyDocument", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(documentDetails.Prefix, "Identification", null, header.MessageId)
            .ConfigureAwait(false);
        await WriteCodeWithCodeListReferenceAttributesAsync("DocumentType", documentDetails.TypeCode, writer).ConfigureAwait(false);

        await writer.WriteElementStringAsync(
                documentDetails.Prefix,
                "Creation",
                null,
                header.TimeStamp.ToString())
            .ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "SenderEnergyParty", null).ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "Identification", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "9").ConfigureAwait(false);
        writer.WriteValue(header.SenderId);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "RecipientEnergyParty", null).ConfigureAwait(false);
        await WriteGlnOrEicCodeWithAttributesAsync("Identification", header.ReceiverId, writer).ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);
        // End HeaderEnergyDocument

        // Begin ProcessEnergyContext
        await writer.WriteStartElementAsync(documentDetails.Prefix, "ProcessEnergyContext", null).ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "EnergyBusinessProcess", null)
            .ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listIdentifier", null, "DK").ConfigureAwait(false);
        writer.WriteValue(EbixCode.Of(BusinessReason.FromName(header.BusinessReason)));
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await WriteCodeWithCodeListReferenceAttributesAsync("EnergyBusinessProcessRole", EbixCode.Of(ActorRole.FromCode(header.ReceiverRole)), writer).ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "EnergyIndustryClassification", null)
            .ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
        writer.WriteValue(GeneralValues.SectorTypeCode);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        if (settlementVersion is not null)
        {
            await writer.WriteStartElementAsync(documentDetails.Prefix, "ProcessVariant", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listIdentifier", null, "DK").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            writer.WriteValue(EbixCode.Of(settlementVersion));
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
        // End ProcessEnergyContext
    }
}
