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
    private readonly Instant _timestamp;
    private readonly List<EnqueuedMessage> _messages = new();
    private MessageHeader _header;
    private MessageType? _documentType;
    private Stream? _generatedDocument;

    #pragma warning disable
    public ReadyMessage(Instant timestamp)
    {
        MessageId = Guid.NewGuid();
        _timestamp = timestamp;
        _header = new MessageHeader(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, _timestamp);
    }

    public ReadyMessage(Instant timestamp, IEnumerable<EnqueuedMessage> messages)
    {
        MessageId = Guid.NewGuid();
        _timestamp = timestamp;
        _header = new MessageHeader(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, _timestamp);
        _messages = messages.ToList();
        CreateHeaderFrom(_messages.First());
        _documentType = EnumerationType.FromName<MessageType>(_messages.First().MessageType);
        ReceiverNumber = ActorNumber.Create(_messages.First().ReceiverId);
        Category = EnumerationType.FromName<MessageCategory>(_messages.First().Category);
    }

    public Guid MessageId { get; }

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

    public void Add(EnqueuedMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (IsFirstMessageInBundle())
        {
            CreateHeaderFrom(message);
            _documentType = EnumerationType.FromName<MessageType>(message.MessageType);
        }

        EnsureProcessType(message);
        EnsureReceiverId(message);

        _messages.Add(message);
    }

    public void Add(OutgoingMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var enqueuedMessage = new EnqueuedMessage(
            message.Id,
            message.ReceiverId.Value,
            message.ReceiverRole.Name,
            message.SenderId.Value,
            message.SenderRole.Name,
            message.MessageType.Name,
            message.MessageType.Category.Name,
            message.ProcessType,
            message.MessageRecord);

        Add(enqueuedMessage);
    }

    public CimMessage CreateMessage()
    {
        if (_messages.Count == 0)
        {
            throw new NoMessagesInBundleException();
        }

        var payloads = _messages.Select(message => message.MessageRecord).ToList();
        return new CimMessage(_documentType!, _header, payloads);
    }

    private void EnsureReceiverId(EnqueuedMessage message)
    {
        if (message.ReceiverId.Equals(_header.ReceiverId, StringComparison.OrdinalIgnoreCase) == false)
        {
            throw new ReceiverIdsDoesNotMatchException(message.Id.ToString());
        }
    }

    private void EnsureProcessType(EnqueuedMessage message)
    {
        if (message.ProcessType.Equals(_header.ProcessType, StringComparison.OrdinalIgnoreCase) == false)
        {
            throw new ProcessTypesDoesNotMatchException(message.Id.ToString());
        }
    }

    private bool IsFirstMessageInBundle()
    {
        return _messages.Count == 0;
    }

    private void CreateHeaderFrom(EnqueuedMessage message)
    {
        _header = new MessageHeader(
            message.ProcessType,
            message.SenderId,
            message.SenderRole,
            message.ReceiverId,
            message.ReceiverRole,
            MessageId.ToString(),
            _timestamp);
    }
}
