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

using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.SeedWork;

namespace Messaging.Domain.OutgoingMessages;

public class ReadyMessage
{
    private Stream? _generatedDocument;

    private ReadyMessage(ReadyMessageId id, ActorNumber receiverNumber, MessageCategory category, IEnumerable<Guid> messageIdsIncluded)
    {
        Id = id;
        ReceiverNumber = receiverNumber;
        Category = category;
        MessageIdsIncluded = messageIdsIncluded;
    }

    public ReadyMessageId Id { get; }

    public ActorNumber ReceiverNumber { get; }

    public MessageCategory Category { get; }

    public IEnumerable<Guid> MessageIdsIncluded { get; }

    public static ReadyMessage CreateFrom(ReadyMessageId id, MessageBundle messageBundle)
    {
        ArgumentNullException.ThrowIfNull(messageBundle);

        return new ReadyMessage(
            id,
            ActorNumber.Create(messageBundle.ReceiverNumber),
            EnumerationType.FromName<MessageCategory>(messageBundle.Category),
            messageBundle.MessageIds);
    }

    public Stream GeneratedDocument()
    {
        return _generatedDocument!;
    }

    public void SetGeneratedDocument(Stream document)
    {
        _generatedDocument = document;
    }
}
