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
using Messaging.Application.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier;
using Processing.Domain.SeedWork;

namespace Messaging.Application.OutgoingMessages;

public class MessageFactory
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ConfirmRequestChangeOfSupplierMessageFactory _confirmRequestChangeOfSupplierMessageFactory;
    private readonly RejectRequestChangeOfSupplierMessageFactory _rejectRequestChangeOfSupplierMessageFactory;

    public MessageFactory(ConfirmRequestChangeOfSupplierMessageFactory confirmRequestChangeOfSupplierMessageFactory, RejectRequestChangeOfSupplierMessageFactory rejectRequestChangeOfSupplierMessageFactory, ISystemDateTimeProvider systemDateTimeProvider)
    {
        _confirmRequestChangeOfSupplierMessageFactory = confirmRequestChangeOfSupplierMessageFactory;
        _rejectRequestChangeOfSupplierMessageFactory = rejectRequestChangeOfSupplierMessageFactory;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public Task<Stream> CreateFromAsync(IReadOnlyCollection<OutgoingMessage> outgoingMessages)
    {
        var firstMessageInList = outgoingMessages.First();
        var processType = ProcessType.FromCode(firstMessageInList.ProcessType);
        return firstMessageInList.DocumentType == "ConfirmRequestChangeOfSupplier"
            ? _confirmRequestChangeOfSupplierMessageFactory.CreateFromAsync(CreateMessageHeaderFrom(firstMessageInList, processType.ReasonCodeForConfirm), outgoingMessages.Select(message => message.MarketActivityRecordPayload).ToList())
            : _rejectRequestChangeOfSupplierMessageFactory.CreateFromAsync(CreateMessageHeaderFrom(firstMessageInList, processType.ReasonCodeForReject), outgoingMessages.Select(message => message.MarketActivityRecordPayload).ToList());
    }

    private MessageHeader CreateMessageHeaderFrom(OutgoingMessage message, string reasonCode)
    {
        return new MessageHeader(message.ProcessType, message.SenderId, message.SenderRole, message.RecipientId, message.ReceiverRole, MessageIdGenerator.Generate(), _systemDateTimeProvider.Now(), reasonCode);
    }
}
