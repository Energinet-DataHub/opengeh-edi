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
using NodaTime;

namespace Messaging.Application.OutgoingMessages;

public class DocumentFactory
{
    private readonly IReadOnlyCollection<IMessageWriter> _documentWriters;

    public DocumentFactory(IEnumerable<IMessageWriter> documentWriters)
    {
        _documentWriters = documentWriters.ToList();
    }

    public Task<Stream> CreateFromAsync(BundledMessageId bundledMessageId, MessageBundle messageBundle, MessageFormat documentFormat, Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(bundledMessageId);
        ArgumentNullException.ThrowIfNull(messageBundle);

        var documentWriter =
            _documentWriters.FirstOrDefault(writer =>
                writer.HandlesType(messageBundle.MessageType) &&
                writer.HandlesFormat(documentFormat));

        if (documentWriter is null)
        {
            throw new OutgoingMessageException($"Could not handle document type {messageBundle.MessageType.Name}");
        }

        return documentWriter.WriteAsync(
            CreateHeader(bundledMessageId, messageBundle, timestamp),
            messageBundle.MessageRecords);
    }

    private static MessageHeader CreateHeader(BundledMessageId bundledMessageId, MessageBundle messageBundle, Instant timeStamp)
    {
        return new MessageHeader(
            messageBundle.ProcessType,
            messageBundle.SenderNumber,
            messageBundle.SenderRole,
            messageBundle.ReceiverNumber,
            messageBundle.ReceiverRole,
            bundledMessageId.Value.ToString(),
            timeStamp);
    }
}
