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
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.Exceptions;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.Queueing;
using NodaTime;

namespace Energinet.DataHub.EDI.Application.OutgoingMessages;

public class DocumentFactory
{
    private readonly IReadOnlyCollection<IDocumentWriter> _documentWriters;

    public DocumentFactory(IEnumerable<IDocumentWriter> documentWriters)
    {
        _documentWriters = documentWriters.ToList();
    }

    public IDocumentWriter GetWriter(DocumentType documentType, DocumentFormat documentFormat)
    {
        var documentWriter = _documentWriters.FirstOrDefault(writer => writer.HandlesType(documentType) && writer.HandlesFormat(documentFormat));

        return documentWriter ?? throw new OutgoingMessageException($"Could not handle document type {documentType} in format {documentFormat}");
    }

    public Task<Stream> CreateFromAsync(OutgoingMessageBundle bundle, DocumentFormat documentFormat, Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(bundle);

        var documentWriter = GetWriter(bundle.DocumentType, documentFormat);

        return documentWriter.WriteAsync(
            new MessageHeader(
                bundle.BusinessReason,
                bundle.SenderId.Value,
                bundle.SenderRole.Name,
                bundle.Receiver.Number.Value,
                bundle.Receiver.ActorRole.Name,
                bundle.AssignedBundleId.Id.ToString(),
                timestamp),
            bundle.OutgoingMessages.Select(message => message.MessageRecord).ToList(),
            bundle.OutgoingMessages.Select(message => message.OriginalData).ToList());
    }

    public async Task<string> CreatePayloadAsync(string payload, DocumentFormat documentFormat, DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(payload);

        var documentWriter =
            _documentWriters.FirstOrDefault(writer =>
            {
                return writer.HandlesType(documentType) &&
                       writer.HandlesFormat(documentFormat);
            }) ?? throw new OutgoingMessageException($"Could not handle document type {documentType} in format {documentFormat}");

        return await documentWriter.WritePayloadAsync(payload).ConfigureAwait(false);
    }
}
