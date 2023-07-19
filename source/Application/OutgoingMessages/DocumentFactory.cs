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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.Exceptions;
using NodaTime;

namespace Application.OutgoingMessages;

public class DocumentFactory
{
    private readonly IReadOnlyCollection<IDocumentWriter> _documentWriters;

    public DocumentFactory(IEnumerable<IDocumentWriter> documentWriters)
    {
        _documentWriters = documentWriters.ToList();
    }

    public Task<Stream> CreateFromAsync(IReadOnlyCollection<OutgoingMessage> outgoingMessages, DocumentFormat documentFormat, Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(outgoingMessages);
        var outgoingMessage = outgoingMessages.First();
        var documentType = outgoingMessage.DocumentType;
        var bundledMessageId = outgoingMessage.AssignedBundleId;
        var senderId = outgoingMessage.SenderId.Value;
        var senderRole = outgoingMessage.SenderRole.Name;
        var receiverId = outgoingMessage.Receiver.Number.Value;
        var receiverRole = outgoingMessage.Receiver.ActorRole.Name;
        var businessReason = outgoingMessage.BusinessReason;

        var documentWriter =
            _documentWriters.FirstOrDefault(writer =>
            {
                return writer.HandlesType(documentType) &&
                       writer.HandlesFormat(documentFormat);
            });

        if (documentWriter is null)
        {
            throw new OutgoingMessageException($"Could not handle document type {documentType}");
        }

        return documentWriter.WriteAsync(
            new MessageHeader(businessReason, senderId, senderRole, receiverId, receiverRole, bundledMessageId!.Id.ToString(), timestamp),
            outgoingMessages.Select(message => message.MessageRecord).ToList());
    }
}
