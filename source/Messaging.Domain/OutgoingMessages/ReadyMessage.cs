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
using NodaTime;

namespace Messaging.Domain.OutgoingMessages;

public class ReadyMessage
{
    private readonly List<EnqueuedMessage> _messages;
    private Stream? _generatedDocument;

    public ReadyMessage(ReadyMessageId id, IEnumerable<EnqueuedMessage> messages)
    {
        Id = id;
        _messages = messages.ToList();
        ReceiverNumber = ActorNumber.Create(_messages.First().ReceiverId);
        Category = EnumerationType.FromName<MessageCategory>(_messages.First().Category);
    }

    public ReadyMessageId Id { get; }

    public Guid MessageId => Id.Value;

    public ActorNumber ReceiverNumber { get; }

    public MessageCategory Category { get; }

    public Stream GeneratedDocument()
    {
        return _generatedDocument!;
    }

    public void SetGeneratedDocument(Stream document)
    {
        _generatedDocument = document;
    }

    public IEnumerable<Guid> GetMessageIdsIncluded()
    {
        return _messages.Select(message => message.Id);
    }
}
