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
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.OutgoingMessages.Common.Xml;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Infrastructure.OutgoingMessages.Common.Xml;

public abstract class MessageWriter : IMessageWriter
{
    private readonly DocumentDetails _documentDetails;
    private readonly IMessageRecordParser _parser;
    private readonly string? _reasonCode;

    protected MessageWriter(DocumentDetails documentDetails, IMessageRecordParser parser, string? reasonCode = null)
    {
        _documentDetails = documentDetails;
        _parser = parser;
        _reasonCode = reasonCode;
    }

    protected DocumentDetails DocumentDetails => _documentDetails;

    public virtual async Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = Encoding.UTF8, Async = true };
        var stream = new MemoryStream();
        using var writer = XmlWriter.Create(stream, settings);
        await WriteHeaderAsync(header, _documentDetails, writer).ConfigureAwait(false);
        await WriteMarketActivityRecordsAsync(marketActivityRecords, writer).ConfigureAwait(false);
        await WriteEndAsync(writer).ConfigureAwait(false);
        stream.Position = 0;
        return stream;
    }

    public bool HandlesType(MessageType messageType)
    {
        if (messageType == null) throw new ArgumentNullException(nameof(messageType));
        return messageType.Name.Equals(_documentDetails.Type.Split("_")[0], StringComparison.OrdinalIgnoreCase);
    }

    public bool HandlesFormat(MessageFormat format)
    {
        return format == MessageFormat.Xml;
    }

    protected abstract Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer);

    protected IReadOnlyCollection<TMarketActivityRecord> ParseFrom<TMarketActivityRecord>(IReadOnlyCollection<string> payloads)
    {
        if (payloads == null) throw new ArgumentNullException(nameof(payloads));
        var marketActivityRecords = new List<TMarketActivityRecord>();
        foreach (var payload in payloads)
        {
            marketActivityRecords.Add(_parser.From<TMarketActivityRecord>(payload));
        }

        return marketActivityRecords;
    }

    protected Task WriteElementAsync(string name, string value, XmlWriter writer)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
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
        if (writer == null) throw new ArgumentNullException(nameof(writer));
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

    private Task WriteHeaderAsync(MessageHeader header, DocumentDetails documentDetails, XmlWriter writer)
    {
        return HeaderWriter.WriteAsync(writer, header, documentDetails, _reasonCode);
    }
}
