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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.Common;
using Processing.Domain.SeedWork;

namespace Messaging.Application.OutgoingMessages;

public class MessageFactory
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IReadOnlyCollection<DocumentWriter> _documentWriters;

    public MessageFactory(
        ISystemDateTimeProvider systemDateTimeProvider,
        IEnumerable<DocumentWriter> documentWriters)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _documentWriters = documentWriters.ToList();
    }

    public Task<Stream> CreateFromAsync(IReadOnlyCollection<OutgoingMessage> outgoingMessages)
    {
        var firstMessageInList = outgoingMessages.First();
        var processType = ProcessType.FromCode(firstMessageInList.ProcessType);
        var documentWriter =
            _documentWriters.First(writer => writer.HandlesDocumentType(firstMessageInList.DocumentType));

        var messageHeader = firstMessageInList.DocumentType == "ConfirmRequestChangeOfSupplier"
            ? CreateMessageHeaderFrom(firstMessageInList, processType.ReasonCodeForConfirm)
            : CreateMessageHeaderFrom(firstMessageInList, processType.ReasonCodeForReject);

        return documentWriter.WriteAsync(
            messageHeader,
            outgoingMessages.Select(message => message.MarketActivityRecordPayload).ToList());
    }

    private MessageHeader CreateMessageHeaderFrom(OutgoingMessage message, string reasonCode)
    {
        return new MessageHeader(message.ProcessType, message.SenderId, message.SenderRole, message.RecipientId, message.ReceiverRole, MessageIdGenerator.Generate(), _systemDateTimeProvider.Now(), reasonCode);
    }
}
