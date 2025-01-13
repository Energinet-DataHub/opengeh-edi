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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM009;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;

public class DocumentFactory
{
    private readonly AcknowledgementJsonDocumentWriter _acknowledgementJsonDocumentWriter;
    private readonly IReadOnlyCollection<IDocumentWriter> _documentWriters;

    public DocumentFactory(
        IEnumerable<IDocumentWriter> documentWriters,
        AcknowledgementJsonDocumentWriter acknowledgementJsonDocumentWriter)
    {
        _acknowledgementJsonDocumentWriter = acknowledgementJsonDocumentWriter;
        _documentWriters = documentWriters.ToList();
    }

    public Task<MarketDocumentStream> CreateFromAsync(
        OutgoingMessageBundle bundle,
        DocumentFormat documentFormat,
        Instant timestamp,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(bundle);

        if (bundle.DocumentType == DocumentType.Acknowledgement)
        {
            if (bundle.OutgoingMessages.Count != 1)
            {
                throw new OutgoingMessageException(
                    $"Could not handle document type {bundle.DocumentType} in format {documentFormat} due to invalid number of messages");
            }

            if (documentFormat == DocumentFormat.Json)
            {
                return _acknowledgementJsonDocumentWriter.WriteAsync(
                    new OutgoingMessageHeader(
                        bundle.BusinessReason,
                        bundle.SenderId.Value,
                        bundle.SenderRole.Code,
                        bundle.Receiver.Number.Value,
                        bundle.DocumentReceiver.ActorRole.Code,
                        bundle.MessageId.Value,
                        timestamp),
                    bundle.OutgoingMessages.Select(message => message.GetSerializedContent()).Single(),
                    cancellationToken);
            }

            if (documentFormat == DocumentFormat.Xml)
            {
            }
            else
            {
                throw new OutgoingMessageException(
                    $"Could not handle document type {bundle.DocumentType} in format {documentFormat}");
            }
        }

        var documentWriter =
            _documentWriters.FirstOrDefault(
                writer => writer.HandlesType(bundle.DocumentType) && writer.HandlesFormat(documentFormat))
            ?? throw new OutgoingMessageException(
                $"Could not handle document type {bundle.DocumentType} in format {documentFormat}");

        return documentWriter.WriteAsync(
            new OutgoingMessageHeader(
                bundle.BusinessReason,
                bundle.SenderId.Value,
                bundle.SenderRole.Code,
                bundle.Receiver.Number.Value,
                bundle.DocumentReceiver.ActorRole.Code,
                bundle.MessageId.Value,
                timestamp),
            bundle.OutgoingMessages.Select(message => message.GetSerializedContent()).ToList(),
            cancellationToken);
    }
}
