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
using System.Threading.Tasks;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Application.OutgoingMessages;

public class DocumentFactory
{
    private readonly IReadOnlyCollection<IDocumentWriter> _documentWriters;

    public DocumentFactory(IEnumerable<IDocumentWriter> documentWriters)
    {
        _documentWriters = documentWriters.ToList();
    }

    public Task<Stream> CreateFromAsync(CimMessage message, CimFormat documentFormat)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var documentWriter =
            _documentWriters.FirstOrDefault(writer =>
                writer.HandlesDocumentType(message.MessageType) &&
                writer.HandlesDocumentFormat(documentFormat));

        if (documentWriter is null)
        {
            throw new OutgoingMessageException($"Could not handle document type {message.MessageType}");
        }

        return documentWriter.WriteAsync(
            message.Header,
            message.MarketActivityRecordPayloads);
    }

    public bool CanHandle(MessageType messageType, CimFormat documentFormat)
    {
        return _documentWriters.Any(writer =>
            writer.HandlesDocumentType(messageType) &&
            writer.HandlesDocumentFormat(documentFormat));
    }
}
