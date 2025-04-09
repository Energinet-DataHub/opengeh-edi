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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;

public class DocumentFactory
{
    private readonly IReadOnlyCollection<IDocumentWriter> _documentWriters;

    public DocumentFactory(IEnumerable<IDocumentWriter> documentWriters)
    {
        _documentWriters = documentWriters.ToList();
    }

    public async Task<MarketDocumentStream> CreateFromAsync(
        OutgoingMessageBundle bundle,
        DocumentFormat documentFormat,
        Instant timestamp,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(bundle);

        var documentWriter =
            _documentWriters.FirstOrDefault(
                writer => writer.HandlesType(bundle.DocumentType)
                          && writer.HandlesFormat(documentFormat))
            ?? throw new OutgoingMessageException(
                $"Could not handle document type {bundle.DocumentType} in format {documentFormat}");

        // var contentTasks = bundle.OutgoingMessages
        //     .Select(message => message.GetContent().ReadAsStringAsync());
        //
        // var payloads = await Task.WhenAll(contentTasks).ConfigureAwait(false);

        var marketDocumentStream = await documentWriter.WriteAsync(
                header: new OutgoingMessageHeader(
                    BusinessReason: bundle.BusinessReason,
                    SenderId: bundle.SenderId.Value,
                    SenderRole: bundle.SenderRole.Code,
                    ReceiverId: bundle.Receiver.Number.Value,
                    ReceiverRole: bundle.DocumentReceiver.ActorRole.Code,
                    MessageId: bundle.MessageId.Value,
                    RelatedToMessageId: bundle.RelatedToMessageId?.Value,
                    TimeStamp: timestamp),
                marketActivityRecords: bundle.OutgoingMessages.Select(message => message.GetContent()).ToList(),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return marketDocumentStream;
    }
}
