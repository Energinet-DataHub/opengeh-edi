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
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Ebix;

public abstract class EbixDocumentWriter : IDocumentWriter
{
    private readonly DocumentDetails _documentDetails;
    private readonly IMessageRecordParser _parser;
    private readonly string? _reasonCode;

    protected EbixDocumentWriter(DocumentDetails documentDetails, IMessageRecordParser parser, string? reasonCode = null)
    {
        _documentDetails = documentDetails;
        _parser = parser;
        _reasonCode = reasonCode;
    }

    protected DocumentDetails DocumentDetails => _documentDetails;

    public virtual async Task<MarketDocumentStream> WriteAsync(OutgoingMessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
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
            marketActivityRecords.AddRange(_parser.From<IList<TMarketActivityRecord>>(payload));
        }

        return marketActivityRecords;
    }

    protected Task WriteElementAsync(string name, string value, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return writer.WriteElementStringAsync(DocumentDetails.Prefix, name, null, value);
    }

    protected async Task WriteEbixCodeWithAttributesAsync(string name, string ebixCode, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(ebixCode);
        ArgumentNullException.ThrowIfNull(writer);

        await writer.WriteStartElementAsync(DocumentDetails.Prefix, name, null).ConfigureAwait(false);
        if (long.TryParse(ebixCode, out _))
        {
            // UN/CEFACT codelist
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
        }
        else if (ebixCode.StartsWith('D') && ebixCode.Length == 3)
        {
            // Danish codelist
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listIdentifier", null, "DK").ConfigureAwait(false);
        }
        else
        {
            // ebIX codelist
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
        }

        await writer.WriteStringAsync(ebixCode).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Will write the ebIX xmlnode with the schemeAgencyIdentifier attribute for a GLN number, EIC code or a GSRN number
    /// </summary>
    /// <param name="name">Name of the xmlnode</param>
    /// <param name="ebixSchemeCode">GLN number, EIC code or a GSRN number</param>
    /// <param name="writer">The XmlWriter</param>
    protected async Task WriteEbixSchemeCodeWithAttributesAsync(string name, string ebixSchemeCode, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(ebixSchemeCode);
        ArgumentNullException.ThrowIfNull(writer);

        await writer.WriteStartElementAsync(DocumentDetails.Prefix, name, null).ConfigureAwait(false);
        if (long.TryParse(ebixSchemeCode, out _))
        {
            if (ebixSchemeCode.Length == 13 || ebixSchemeCode.Length == 18)
            {
                // GLN or GSNR number from GS1
                await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "9").ConfigureAwait(false);
            }
            else if (ebixSchemeCode.Length == 16)
            {
                // EIC code
                await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "305").ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException($"Invalid schemecode '{ebixSchemeCode}'");
            }
        }
        else
        {
            throw new InvalidOperationException($"Invalid schemecode '{ebixSchemeCode}'");
        }

        await writer.WriteStringAsync(ebixSchemeCode).ConfigureAwait(false);
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

    private Task WriteHeaderAsync(OutgoingMessageHeader header, DocumentDetails documentDetails, XmlWriter writer, SettlementVersion? settlementVersion)
    {
        return EbixHeaderWriter.WriteAsync(writer, header, documentDetails, _reasonCode, settlementVersion);
    }
}
