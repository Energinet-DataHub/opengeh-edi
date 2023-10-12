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
using Energinet.DataHub.EDI.Application.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Application.OutgoingMessages.Common.Xml;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;

namespace Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common.Ebix;

public abstract class EbixDocumentWriter : DocumentWriter
{
    private readonly DocumentDetails _documentDetails;
    private readonly string? _reasonCode;

    protected EbixDocumentWriter(DocumentDetails documentDetails, IMessageRecordParser parser, string? reasonCode = null)
        : base(parser)
    {
        _documentDetails = documentDetails;
        _reasonCode = reasonCode;
    }

    internal DocumentDetails DocumentDetails => _documentDetails;

    internal string? ReasonCode => _reasonCode;

    public override async Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords, IReadOnlyCollection<string> originalData)
    {
        ArgumentNullException.ThrowIfNull(marketActivityRecords);
        var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = new UTF8Encoding(false), Async = true, Indent = true };
        var stream = new MemoryStream();
        using var writer = XmlWriter.Create(stream, settings);
        SettlementVersion? settlementVersion = ExtractSettlementVersion(originalData);
        await WriteHeaderAsync(header, DocumentDetails, writer, settlementVersion).ConfigureAwait(false);
        foreach (var marketActivityRecord in marketActivityRecords)
        {
            await writer.WriteRawAsync(marketActivityRecord).ConfigureAwait(false);
        }

        await WriteEndAsync(writer).ConfigureAwait(false);
        stream.Position = 0;
        return stream;
    }

    public override bool HandlesFormat(DocumentFormat format)
    {
        return format == DocumentFormat.Ebix;
    }

    public override async Task<string> WritePayloadAsync(string marketActivityRecord)
    {
        return await WritePayloadInternalAsync(marketActivityRecord).ConfigureAwait(false);
    }

    public override async Task<string> WritePayloadAsync<T>(object data)
    {
        return await WritePayloadInternalAsync(Parser.From((T)data)).ConfigureAwait(false);
    }

    protected abstract Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer);

    protected virtual SettlementVersion? ExtractSettlementVersion(IReadOnlyCollection<string> marketActivityPayloads)
    {
        return null;
    }

    //protected abstract Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer);

    //protected IReadOnlyCollection<TMarketActivityRecord> ParseFrom<TMarketActivityRecord>(IReadOnlyCollection<string> payloads)
    //{
    //    if (payloads == null)
    //        throw new ArgumentNullException(nameof(payloads));
    //    var marketActivityRecords = new List<TMarketActivityRecord>();
    //    foreach (var payload in payloads)
    //    {
    //        marketActivityRecords.Add(_parser.From<TMarketActivityRecord>(payload));
    //    }

    //    return marketActivityRecords;
    //}

    protected Task WriteElementAsync(string name, string value, XmlWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        return writer.WriteElementStringAsync(DocumentDetails.Prefix, name, null, value);
    }

    protected Task WriteElementIfHasValueAsync(string name, string? value, XmlWriter writer)
    {
        if (!string.IsNullOrEmpty(value))
        {
            return WriteElementAsync(name, value, writer);
        }

        return Task.CompletedTask;
    }

    protected async Task WriteMridAsync(string localName, string id, string codingScheme, XmlWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, localName, null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, codingScheme).ConfigureAwait(false);
        writer.WriteValue(id);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteEndAsync(XmlWriter writer)
    {
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        writer.Close();
    }

    private Task WriteHeaderAsync(MessageHeader header, DocumentDetails documentDetails, XmlWriter writer, SettlementVersion? settlementVersion)
    {
        return EbixHeaderWriter.WriteAsync(writer, header, documentDetails, ReasonCode, settlementVersion);
    }

    private async Task<string> WritePayloadInternalAsync(string marketActivityRecord)
    {
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Encoding = new UTF8Encoding(false), Async = true, Indent = true };
        var stream = new MemoryStream();
        using var writer = XmlWriter.Create(stream, settings);
        await writer.WriteStartDocumentAsync().ConfigureAwait(false);
        await writer.WriteStartElementAsync(
            DocumentDetails.Prefix,
            DocumentDetails.Type,
            DocumentDetails.XmlNamespace).ConfigureAwait(false);

        await WriteMarketActivityRecordsAsync(new List<string>() { marketActivityRecord }, writer).ConfigureAwait(false);
        await WriteEndAsync(writer).ConfigureAwait(false);

        stream.Position = 0;

        var xmlDocument = new XmlDocument();
        xmlDocument.Load(stream);
        var ret = xmlDocument.DocumentElement?.InnerXml;

        var ns = $" xmlns:{DocumentDetails.Prefix}=\"{DocumentDetails.XmlNamespace}\""; //xmlns:cim=\"urn:ediel.org:measure:notifyaggregatedmeasuredata:0:1\
        ret = ret?.Replace(ns, string.Empty, StringComparison.InvariantCultureIgnoreCase);
        return ret ?? string.Empty;
    }
}
