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

using Messaging.Domain.SeedWork;
using NodaTime;

namespace Messaging.Domain.OutgoingMessages;

public class Bundle
{
    private readonly Instant _timestamp;
    private readonly List<OutgoingMessage> _messages = new();
    private MessageHeader _header;
    private DocumentType? _documentType;

    public Bundle(Instant timestamp)
    {
        _timestamp = timestamp;
        _header = new MessageHeader(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, _timestamp);
    }

    public void Add(OutgoingMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (IsFirstMessageInBundle())
        {
            CreateHeaderFrom(message);
            _documentType = message.DocumentType;
        }

        EnsureProcessType(message);
        EnsureReceiverId(message);

        _messages.Add(message);
    }

    public CimMessage CreateMessage()
    {
        if (_messages.Count == 0)
        {
            throw new NoMessagesInBundleException();
        }

        var payloads = _messages.Select(message => message.MarketActivityRecordPayload).ToList();
        return new CimMessage(_documentType!, _header, payloads);
    }

    private void EnsureReceiverId(OutgoingMessage message)
    {
        if (message.ReceiverId.Value.Equals(_header.ReceiverId, StringComparison.OrdinalIgnoreCase) == false)
        {
            throw new ReceiverIdsDoesNotMatchException(message.Id.ToString());
        }
    }

    private void EnsureProcessType(OutgoingMessage message)
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

    private void CreateHeaderFrom(OutgoingMessage message)
    {
        _header = new MessageHeader(
            message.ProcessType,
            message.SenderId.Value,
            message.SenderRole.ToString(),
            message.ReceiverId.Value,
            message.ReceiverRole.ToString(),
            Guid.NewGuid().ToString(),
            _timestamp);
    }
}
